using HRStackPK.Api.DAL;
using HRStackPK.Api.Helpers;
using HRStackPK.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRStackPK.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/attendance")]
public class AttendanceController : ControllerBase
{
    private static readonly TimeSpan LateAfter = new(9, 15, 0);   // business rule: Late after 09:15

    private readonly AttendanceDAL _dal;
    public AttendanceController(AttendanceDAL dal) => _dal = dal;

    /// <summary>GET /api/attendance?date=yyyy-MM-dd — all marks for a day (Head/HR web + mobile).</summary>
    [HttpGet]
    public IActionResult ByDate([FromQuery] string? date)
    {
        var d = string.IsNullOrWhiteSpace(date) ? DateTime.Today.ToString("yyyy-MM-dd") : date;
        return Ok(_dal.ByDate(User.CompanyId(), User.BranchId(), d));
    }

    /// <summary>POST /api/attendance/mark — Head/HR marks (upsert); Absent clears times.</summary>
    [HttpPost("mark")]
    public IActionResult Mark([FromBody] MarkAttendanceRequest req)
    {
        if (req.EmployeeId <= 0)                       return BadRequest(new { message = "employeeId is required" });
        if (string.IsNullOrWhiteSpace(req.Date))       return BadRequest(new { message = "date is required" });
        if (string.IsNullOrWhiteSpace(req.Status))     return BadRequest(new { message = "status is required" });

        var row = _dal.Mark(User.CompanyId(), User.BranchId(), req, User.EmployeeIdOrNull());
        return Ok(row);
    }

    /// <summary>GET /api/attendance/me?from=&to= — staff's own history (mobile).</summary>
    [HttpGet("me")]
    public IActionResult MyHistory([FromQuery] string? from, [FromQuery] string? to)
    {
        var empId = User.EmployeeId();
        if (empId == 0) return BadRequest(new { message = "Head account has no attendance" });
        return Ok(_dal.History(User.CompanyId(), empId, from, to));
    }

    /// <summary>GET /api/attendance/employee/{id}?from=&to= — any employee's history (Head/HR).</summary>
    [HttpGet("employee/{id:long}")]
    public IActionResult History(long id, [FromQuery] string? from, [FromQuery] string? to) =>
        Ok(_dal.History(User.CompanyId(), id, from, to));

    /// <summary>POST /api/attendance/check-in — mobile staff self check-in. Late after 09:15.</summary>
    [HttpPost("check-in")]
    public IActionResult CheckIn()
    {
        var empId = User.EmployeeId();
        if (empId == 0) return BadRequest(new { message = "Only staff can check in" });

        var now = DateTime.Now;
        var req = new MarkAttendanceRequest
        {
            EmployeeId = empId,
            Date       = now.ToString("yyyy-MM-dd"),
            Status     = now.TimeOfDay > LateAfter ? "Late" : "Present",
            InTime     = now.ToString("HH:mm")
        };
        var row = _dal.Mark(User.CompanyId(), User.BranchId(), req, empId);
        return Ok(row);
    }
}
