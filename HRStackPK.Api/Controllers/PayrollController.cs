using HRStackPK.Api.DAL;
using HRStackPK.Api.Helpers;
using HRStackPK.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HRStackPK.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payroll")]
public class PayrollController : ControllerBase
{
    private readonly PayrollDAL _dal;
    public PayrollController(PayrollDAL dal) => _dal = dal;

    /// <summary>GET /api/payroll/overrides?month=2026-07 → {"5": {basic: ..., hra: ...}, ...}</summary>
    [HttpGet("overrides")]
    public IActionResult GetOverrides([FromQuery] string? month)
    {
        var (m, y) = MonthX.Parse(month);
        return Ok(_dal.GetOverrides(User.CompanyId(), User.BranchId(), m, y));
    }

    /// <summary>PUT /api/payroll/overrides/{employeeId} — body with any of 8 fields; all-null = Reset to auto.</summary>
    [HttpPut("overrides/{employeeId:long}")]
    public IActionResult PutOverride(long employeeId, [FromBody] OverridePutRequest? req)
    {
        var (m, y) = MonthX.Parse(req?.Month);

        if (req is null || req.IsEmpty())
        {
            _dal.ResetOverride(User.CompanyId(), employeeId, m, y);
            return Ok(new { message = "Reset to auto", employeeId, month = MonthX.Format(m, y) });
        }

        _dal.UpsertOverride(User.CompanyId(), User.BranchId(), employeeId, m, y, req);
        return Ok(new { message = "Override saved", employeeId, month = MonthX.Format(m, y) });
    }

    /// <summary>POST /api/payroll/generate {month, rows} — the ONLY creator of payslips.
    /// Also writes salary expense transactions and decrements active loans.</summary>
    [HttpPost("generate")]
    public IActionResult Generate([FromBody] PayrollGenerateRequest req)
    {
        if (req.Rows.Count == 0) return BadRequest(new { message = "rows are required" });

        var (m, y) = MonthX.Parse(req.Month);
        try
        {
            var (runId, count) = _dal.Generate(User.CompanyId(), User.BranchId(), m, y,
                                               User.EmployeeIdOrNull(), req.Rows);
            return Ok(new
            {
                message = $"Payroll generated — {count} payslips",
                payrollRunId = runId,
                slipCount = count,
                month = MonthX.Format(m, y)
            });
        }
        catch (SqlException ex) when (ex.Number == 51002)
        {
            return Conflict(new { message = $"Payroll already generated for {MonthX.Format(m, y)}" });
        }
    }
}

[ApiController]
[Authorize]
[Route("api/payslips")]
public class PayslipsController : ControllerBase
{
    private readonly PayslipsDAL _dal;
    public PayslipsController(PayslipsDAL dal) => _dal = dal;

    /// <summary>Admin — all payslips of the branch.</summary>
    [HttpGet]
    public IActionResult List() =>
        Ok(_dal.List(User.CompanyId(), User.BranchId()));

    /// <summary>Staff — own payslip history.</summary>
    [HttpGet("me")]
    public IActionResult Me()
    {
        var empId = User.EmployeeId();
        if (empId == 0) return BadRequest(new { message = "Head account has no payslips" });
        return Ok(_dal.ByEmployee(User.CompanyId(), empId));
    }

    /// <summary>Head/HR — one employee's payslip history (mobile: tap employee → slips).</summary>
    [HttpGet("employee/{id:long}")]
    public IActionResult ByEmployee(long id) =>
        Ok(_dal.ByEmployee(User.CompanyId(), id));

    /// <summary>GET /api/payslips/overview?month=2026-07 — Generated / Not-generated per employee.</summary>
    [HttpGet("overview")]
    public IActionResult Overview([FromQuery] string? month)
    {
        var (m, y) = MonthX.Parse(month);
        return Ok(_dal.Overview(User.CompanyId(), User.BranchId(), m, y));
    }
}
