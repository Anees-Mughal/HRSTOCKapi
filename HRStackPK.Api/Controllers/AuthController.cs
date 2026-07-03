using System.Text.RegularExpressions;
using HRStackPK.Api.DAL;
using HRStackPK.Api.Helpers;
using HRStackPK.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HRStackPK.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private static readonly Regex MobileRx = new(@"^03\d{2}-?\d{7}$");
    private static readonly Regex EmailRx  = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private readonly AuthDAL _dal;
    private readonly JwtHelper _jwt;

    public AuthController(AuthDAL dal, JwtHelper jwt)
    {
        _dal = dal;
        _jwt = jwt;
    }

    // =================================================================
    // POST /api/auth/signup — company + main branch + head account
    // =================================================================
    [HttpPost("signup")]
    public IActionResult Signup([FromBody] SignupRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.CompanyName)) return BadRequest(new { message = "Company name is required" });
        if (string.IsNullOrWhiteSpace(req.OwnerName))   return BadRequest(new { message = "Owner name is required" });
        if (!MobileRx.IsMatch(req.Phone.Trim()))        return BadRequest(new { message = "Enter a valid mobile number (03xx-xxxxxxx)" });
        if (!EmailRx.IsMatch(req.Email.Trim()))         return BadRequest(new { message = "Enter a valid email" });
        if ((req.Password ?? "").Length < 6)            return BadRequest(new { message = "Password must be at least 6 characters" });

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        long companyId, branchId;
        try
        {
            (companyId, branchId) = _dal.Signup(req, hash);
        }
        catch (SqlException ex) when (ex.Number == 51001)
        {
            return Conflict(new { message = "This mobile number is already registered" });
        }

        var user = new AuthUser
        {
            Id         = companyId,
            Identifier = AuthDAL.NormalizeMobile(req.Phone),
            Role       = "Head",
            RoleId     = 1,
            TenantId   = companyId,
            BranchId   = branchId,
            FullName   = req.OwnerName.Trim(),
            EmployeeId = null
        };

        return Ok(IssueTokens(user, loginType: "Head"));
    }

    // =================================================================
    // POST /api/auth/login — mobile → Head (Companies), email → Staff (Employees)
    // =================================================================
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var identifier = (req.Identifier ?? "").Trim();
        if (identifier.Length == 0) return BadRequest(new { message = "Mobile number or email is required" });

        if (MobileRx.IsMatch(identifier))
            return LoginHead(identifier, req.Password ?? "");

        if (EmailRx.IsMatch(identifier))
            return LoginStaff(identifier, req.Password ?? "");

        return BadRequest(new { message = "Enter a valid mobile number or email" });
    }

    private IActionResult LoginHead(string mobile, string password)
    {
        var head = _dal.GetHeadByMobile(mobile);
        if (head is null || !head.IsActive || !BCrypt.Net.BCrypt.Verify(password, head.Password))
            return Unauthorized(new { message = "Invalid credentials" });

        var user = new AuthUser
        {
            Id         = head.CompanyID,
            Identifier = head.Mobile,
            Role       = "Head",
            RoleId     = 1,
            TenantId   = head.CompanyID,
            BranchId   = head.BranchID,
            FullName   = head.CompanyName,
            EmployeeId = null
        };
        return Ok(IssueTokens(user, loginType: "Head"));
    }

    private IActionResult LoginStaff(string email, string password)
    {
        var staff = _dal.GetStaffByEmail(email);
        if (staff is null || !staff.IsActive || !BCrypt.Net.BCrypt.Verify(password, staff.Password))
            return Unauthorized(new { message = "Invalid credentials" });

        var user = new AuthUser
        {
            Id         = staff.EmployeeID,
            Identifier = staff.Email,
            Role       = staff.RoleName,
            RoleId     = staff.RoleID,
            TenantId   = staff.CompanyID,
            BranchId   = staff.BranchID,
            FullName   = staff.FullName,
            EmployeeId = staff.EmployeeID
        };
        return Ok(IssueTokens(user, loginType: "Staff"));
    }

    // =================================================================
    // POST /api/auth/refresh — rotate refresh token, new access token
    // =================================================================
    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            return BadRequest(new { message = "refreshToken is required" });

        var row = _dal.GetRefreshToken(req.RefreshToken);
        if (row is null || row.IsRevoked || row.ExpiresAt <= DateTime.UtcNow)
            return Unauthorized(new { message = "Session expired — please sign in again" });

        AuthUser user;
        string loginType;

        if (row.IsHeadLogin)
        {
            var head = _dal.GetHeadByCompanyId(row.CompanyID);
            if (head is null || !head.IsActive)
                return Unauthorized(new { message = "Account inactive" });

            user = new AuthUser
            {
                Id = head.CompanyID, Identifier = head.Mobile, Role = "Head", RoleId = 1,
                TenantId = head.CompanyID, BranchId = head.BranchID,
                FullName = head.CompanyName, EmployeeId = null
            };
            loginType = "Head";
        }
        else
        {
            var staff = _dal.GetStaffById(row.EmployeeID!.Value);
            if (staff is null || !staff.IsActive)
                return Unauthorized(new { message = "Account inactive" });

            user = new AuthUser
            {
                Id = staff.EmployeeID, Identifier = staff.Email, Role = staff.RoleName, RoleId = staff.RoleID,
                TenantId = staff.CompanyID, BranchId = staff.BranchID,
                FullName = staff.FullName, EmployeeId = staff.EmployeeID
            };
            loginType = "Staff";
        }

        _dal.RevokeRefreshToken(req.RefreshToken);   // rotation — old token dies
        return Ok(IssueTokens(user, loginType));
    }

    // =================================================================
    // POST /api/auth/logout — revoke refresh token
    // =================================================================
    [HttpPost("logout")]
    public IActionResult Logout([FromBody] RefreshRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.RefreshToken))
            _dal.RevokeRefreshToken(req.RefreshToken);
        return Ok(new { message = "Signed out" });
    }

    // =================================================================
    private AuthResponse IssueTokens(AuthUser user, string loginType)
    {
        var access  = _jwt.CreateAccessToken(user, loginType);
        var refresh = JwtHelper.CreateRefreshToken();

        _dal.SaveRefreshToken(
            companyId:   user.TenantId,
            employeeId:  user.EmployeeId,
            isHeadLogin: loginType == "Head",
            token:       refresh,
            expiresAt:   DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays));

        return new AuthResponse { AccessToken = access, RefreshToken = refresh, User = user };
    }

    // =================================================================
    // POST /api/auth/change-password — logged-in user (Head OR staff)
    // =================================================================
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("change-password")]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.CurrentPassword))
            return BadRequest(new { message = "Current password is required" });
        if ((req.NewPassword ?? "").Length < 6)
            return BadRequest(new { message = "New password must be at least 6 characters" });

        var companyId = User.CompanyId();

        if (User.IsHead())
        {
            var head = _dal.GetHeadByCompanyId(companyId);
            if (head is null) return NotFound(new { message = "Account not found" });
            if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, head.Password))
                return BadRequest(new { message = "Current password is incorrect" });

            _dal.UpdateHeadPassword(companyId, BCrypt.Net.BCrypt.HashPassword(req.NewPassword));
        }
        else
        {
            var empId = User.EmployeeId();
            var staff = _dal.GetStaffById(empId);
            if (staff is null || staff.CompanyID != companyId)
                return NotFound(new { message = "Account not found" });
            if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, staff.Password))
                return BadRequest(new { message = "Current password is incorrect" });

            _dal.UpdateStaffPassword(companyId, empId, BCrypt.Net.BCrypt.HashPassword(req.NewPassword));
        }

        return Ok(new { message = "Password changed successfully" });
    }

    ///// <summary>Head/HR resets a staff password. Default "123456" if body empty.</summary>
    //[HttpPost("{id:long}/reset-password")]
    //public IActionResult ResetPassword(long id, [FromBody] ResetPasswordRequest? req, [FromServices] AuthDAL auth)
    //{
    //    var emp = _dal.GetById(User.CompanyId(), id);
    //    if (emp is null) return NotFound();

    //    var newPass = string.IsNullOrWhiteSpace(req?.NewPassword) ? "123456" : req!.NewPassword!;
    //    if (newPass.Length < 6) return BadRequest(new { message = "Password must be at least 6 characters" });

    //    auth.UpdateStaffPassword(User.CompanyId(), id, BCrypt.Net.BCrypt.HashPassword(newPass));
    //    return Ok(new { message = $"Password reset for {emp.FullName}" });
    //}
}
