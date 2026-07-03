using HRStackPK.Api.DAL;
using HRStackPK.Api.Helpers;
using HRStackPK.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HRStackPK.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/leaves")]
public class LeavesController : ControllerBase
{
    private readonly LeavesDAL _dal;
    public LeavesController(LeavesDAL dal) => _dal = dal;

    [HttpGet]
    public IActionResult List() =>
        Ok(_dal.List(User.CompanyId(), User.BranchId()));

    [HttpGet("types")]
    public IActionResult Types() =>
        Ok(_dal.Types(User.CompanyId()));

    [HttpGet("me")]
    public IActionResult Me()
    {
        var empId = User.EmployeeId();
        if (empId == 0) return BadRequest(new { message = "Head account has no leaves" });
        return Ok(_dal.Me(User.CompanyId(), empId));
    }

    [HttpGet("me/balances")]
    public IActionResult MyBalances()
    {
        var empId = User.EmployeeId();
        if (empId == 0) return BadRequest(new { message = "Head account has no leave balances" });
        return Ok(_dal.BalancesMe(User.CompanyId(), User.BranchId(), empId, DateTime.Today.Year));
    }

    [HttpPost]
    public IActionResult Apply([FromBody] ApplyLeaveRequest req)
    {
        if (req.TypeId <= 0)                       return BadRequest(new { message = "typeId is required" });
        if (string.IsNullOrWhiteSpace(req.From))   return BadRequest(new { message = "from date is required" });
        if (string.IsNullOrWhiteSpace(req.To))     return BadRequest(new { message = "to date is required" });
        if (req.Days <= 0)                         return BadRequest(new { message = "days must be positive" });

        // staff apply for self; Head/HR can apply for an employee
        var empId = req.EmployeeId is > 0 && User.IsHead() ? req.EmployeeId.Value : User.EmployeeId();
        if (empId == 0) return BadRequest(new { message = "employeeId is required" });

        return Ok(_dal.Apply(User.CompanyId(), User.BranchId(), empId, req));
    }

    [HttpPut("{id:long}/status")]
    public IActionResult SetStatus(long id, [FromBody] StatusRequest req)
    {
        if (req.Status is not ("Approved" or "Rejected"))
            return BadRequest(new { message = "status must be Approved or Rejected" });
        try
        {
            var leave = _dal.SetStatus(User.CompanyId(), id, req.Status,
                                       User.EmployeeIdOrNull(), req.RejectionReason);
            return leave is null ? NotFound() : Ok(leave);
        }
        catch (SqlException ex) when (ex.Number == 51004)
        {
            return NotFound(new { message = "Leave application not found" });
        }
    }
}

[ApiController]
[Authorize]
[Route("api/loans")]
public class LoansController : ControllerBase
{
    private readonly LoansDAL _dal;
    public LoansController(LoansDAL dal) => _dal = dal;

    [HttpGet]
    public IActionResult List() =>
        Ok(_dal.List(User.CompanyId(), User.BranchId()));

    [HttpGet("me")]
    public IActionResult Me()
    {
        var empId = User.EmployeeId();
        if (empId == 0) return BadRequest(new { message = "Head account has no loans" });
        return Ok(_dal.Me(User.CompanyId(), empId));
    }

    [HttpPost]
    public IActionResult Apply([FromBody] ApplyLoanRequest req)
    {
        if (req.Amount <= 0)     return BadRequest(new { message = "amount must be positive" });
        if (req.Instalment <= 0) return BadRequest(new { message = "instalment must be positive" });

        var empId = req.EmployeeId is > 0 && User.IsHead() ? req.EmployeeId.Value : User.EmployeeId();
        if (empId == 0) return BadRequest(new { message = "employeeId is required" });

        return Ok(_dal.Apply(User.CompanyId(), User.BranchId(), empId, req));
    }

    [HttpPut("{id:long}/status")]
    public IActionResult SetStatus(long id, [FromBody] StatusRequest req)
    {
        if (req.Status is not ("Approved" or "Rejected"))
            return BadRequest(new { message = "status must be Approved or Rejected" });

        var loan = _dal.SetStatus(User.CompanyId(), id, req.Status,
                                  User.EmployeeIdOrNull(), req.RejectionReason);
        return loan is null ? NotFound() : Ok(loan);
    }
}
