using HRStackPK.Api.DAL;
using HRStackPK.Api.Helpers;
using HRStackPK.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRStackPK.Api.Controllers;

[ApiController]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly AccountsDAL _dal;
    public AccountsController(AccountsDAL dal) => _dal = dal;

    /// <summary>GET /api/expenses?month=2026-07 — manual + auto salary expenses.</summary>
    [HttpGet("api/expenses")]
    public IActionResult Expenses([FromQuery] string? month)
    {
        int? m = null, y = null;
        if (!string.IsNullOrWhiteSpace(month)) { var (mm, yy) = MonthX.Parse(month); m = mm; y = yy; }
        return Ok(_dal.Expenses(User.CompanyId(), User.BranchId(), m, y));
    }

    [HttpPost("api/expenses")]
    public IActionResult CreateExpense([FromBody] CreateExpenseRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Category)) return BadRequest(new { message = "category is required" });
        if (req.Amount <= 0)                         return BadRequest(new { message = "amount must be positive" });
        return Ok(_dal.CreateExpense(User.CompanyId(), User.BranchId(), req, User.EmployeeIdOrNull()));
    }

    /// <summary>GET /api/revenues?month=2026-07</summary>
    [HttpGet("api/revenues")]
    public IActionResult Revenues([FromQuery] string? month)
    {
        int? m = null, y = null;
        if (!string.IsNullOrWhiteSpace(month)) { var (mm, yy) = MonthX.Parse(month); m = mm; y = yy; }
        return Ok(_dal.Revenues(User.CompanyId(), User.BranchId(), m, y));
    }

    [HttpPost("api/revenues")]
    public IActionResult CreateRevenue([FromBody] CreateRevenueRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Source)) return BadRequest(new { message = "source is required" });
        if (req.Amount <= 0)                       return BadRequest(new { message = "amount must be positive" });
        return Ok(_dal.CreateRevenue(User.CompanyId(), User.BranchId(), req, User.EmployeeIdOrNull()));
    }

    /// <summary>GET /api/accounts/report?month=2026-07 — mobile Reports screen + web Accounts Reports tab.
    /// {month, expenses:{total,groups}, revenue:{total,groups}, pl:{revenue,expenses,net}, salaryCount}</summary>
    [HttpGet("api/accounts/report")]
    public IActionResult Report([FromQuery] string? month)
    {
        var (m, y) = MonthX.Parse(month);
        return Ok(_dal.Report(User.CompanyId(), User.BranchId(), m, y));
    }
}

[ApiController]
[Authorize]
[Route("api/appraisals")]
public class AppraisalsController : ControllerBase
{
    private readonly AppraisalsDAL _dal;
    public AppraisalsController(AppraisalsDAL dal) => _dal = dal;

    [HttpGet]
    public IActionResult List() =>
        Ok(_dal.List(User.CompanyId(), User.BranchId()));

    [HttpPost]
    public IActionResult Create([FromBody] AppraisalCreateRequest req)
    {
        if (req.EmployeeId <= 0)                  return BadRequest(new { message = "employeeId is required" });
        if (string.IsNullOrWhiteSpace(req.Cycle)) return BadRequest(new { message = "cycle is required" });
        return Ok(_dal.Create(User.CompanyId(), User.BranchId(), req));
    }
}
