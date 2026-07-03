using HRStackPK.Api.DAL;
using HRStackPK.Api.Helpers;
using HRStackPK.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HRStackPK.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly EmployeesDAL _dal;
    public EmployeesController(EmployeesDAL dal) => _dal = dal;

    [HttpGet]
    public IActionResult List() =>
        Ok(_dal.List(User.CompanyId(), User.BranchId()));

    /// <summary>Staff self-service profile (mobile Profile screen / web /me).</summary>
    [HttpGet("me")]
    public IActionResult Me()
    {
        var empId = User.EmployeeId();
        if (empId == 0) return BadRequest(new { message = "Head account has no employee profile" });
        var emp = _dal.GetById(User.CompanyId(), empId);
        return emp is null ? NotFound() : Ok(emp);
    }

    [HttpGet("{id:long}")]
    public IActionResult Get(long id)
    {
        var emp = _dal.GetById(User.CompanyId(), id);
        return emp is null ? NotFound() : Ok(emp);
    }

    [HttpPost]
    public IActionResult Create([FromBody] EmployeeUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest(new { message = "Full name is required" });
        if (string.IsNullOrWhiteSpace(req.Email))    return BadRequest(new { message = "Email is required" });

        var hash = BCrypt.Net.BCrypt.HashPassword(
            string.IsNullOrWhiteSpace(req.Password) ? "123456" : req.Password);
        try
        {
            return Ok(_dal.Create(User.CompanyId(), User.BranchId(), req, hash));
        }
        catch (SqlException ex) when (ex.Number == 51003)
        {
            return Conflict(new { message = "This email is already registered" });
        }
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, [FromBody] EmployeeUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest(new { message = "Full name is required" });
        try
        {
            var emp = _dal.Update(User.CompanyId(), id, req);
            return emp is null ? NotFound() : Ok(emp);
        }
        catch (SqlException ex) when (ex.Number == 51003)
        {
            return Conflict(new { message = "This email is already registered" });
        }
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id)
    {
        _dal.Delete(User.CompanyId(), id);
        return Ok(new { message = "Employee deactivated" });
    }
}

[ApiController]
[Authorize]
[Route("api/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly DepartmentsDAL _dal;
    public DepartmentsController(DepartmentsDAL dal) => _dal = dal;

    [HttpGet]
    public IActionResult List() =>
        Ok(_dal.List(User.CompanyId(), User.BranchId()));

    [HttpPost]
    public IActionResult Create([FromBody] DepartmentUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required" });
        return Ok(_dal.Create(User.CompanyId(), User.BranchId(), req));
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, [FromBody] DepartmentUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required" });
        var dept = _dal.Update(User.CompanyId(), id, req);
        return dept is null ? NotFound() : Ok(dept);
    }
}
