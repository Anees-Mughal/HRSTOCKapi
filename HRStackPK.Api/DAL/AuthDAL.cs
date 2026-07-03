using HRStackPK.Api.Models;
using Microsoft.Data.SqlClient;

namespace HRStackPK.Api.DAL;

public class AuthDAL
{
    private readonly Db _db;
    public AuthDAL(Db db) => _db = db;

    /// <summary>Creates Company + main Branch + seeds LeaveTypes + default RolePermissions. Returns (CompanyID, BranchID). Throws SqlException 51001 if mobile taken.</summary>
    public (long companyId, long branchId) Signup(SignupRequest req, string passwordHash)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Auth_Signup",
            ("@CompanyName", req.CompanyName.Trim()),
            ("@OwnerName",   req.OwnerName.Trim()),
            ("@Email",       req.Email.Trim()),
            ("@Mobile",      NormalizeMobile(req.Phone)),
            ("@PasswordHash", passwordHash));

        using var r = cmd.ExecuteReader();
        r.Read();
        return (Db.L(r, "CompanyID"), Db.L(r, "BranchID"));
    }

    public HeadRow? GetHeadByMobile(string mobile)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Auth_GetHeadByMobile", ("@Mobile", NormalizeMobile(mobile)));
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new HeadRow
        {
            CompanyID   = Db.L(r, "CompanyID"),
            CompanyName = Db.S(r, "CompanyName"),
            Mobile      = Db.S(r, "Mobile"),
            Password    = Db.S(r, "Password"),
            BranchID    = Db.L(r, "BranchID"),
            IsActive    = Db.B(r, "IsActive")
        };
    }

    public StaffRow? GetStaffByEmail(string email)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Auth_GetStaffByEmail", ("@Email", email.Trim()));
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new StaffRow
        {
            EmployeeID = Db.L(r, "EmployeeID"),
            CompanyID  = Db.L(r, "CompanyID"),
            BranchID   = Db.L(r, "BranchID"),
            FullName   = Db.S(r, "FullName"),
            Email      = Db.S(r, "Email"),
            Password   = Db.S(r, "Password"),
            IsActive   = Db.B(r, "IsActive"),
            RoleID     = Db.L(r, "RoleID"),
            RoleName   = Db.S(r, "RoleName")
        };
    }

    public void SaveRefreshToken(long companyId, long? employeeId, bool isHeadLogin, string token, DateTime expiresAt)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_RefreshToken_Save",
            ("@CompanyID",   companyId),
            ("@EmployeeID",  employeeId),
            ("@IsHeadLogin", isHeadLogin),
            ("@Token",       token),
            ("@ExpiresAt",   expiresAt));
        cmd.ExecuteNonQuery();
    }

    public RefreshTokenRow? GetRefreshToken(string token)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_RefreshToken_Get", ("@Token", token));
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new RefreshTokenRow
        {
            ID          = Db.L(r, "ID"),
            CompanyID   = Db.L(r, "CompanyID"),
            EmployeeID  = Db.Ln(r, "EmployeeID"),
            IsHeadLogin = Db.B(r, "IsHeadLogin"),
            Token       = Db.S(r, "Token"),
            ExpiresAt   = Db.D(r, "ExpiresAt"),
            IsRevoked   = Db.B(r, "IsRevoked")
        };
    }

    public void RevokeRefreshToken(string token)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_RefreshToken_Revoke", ("@Token", token));
        cmd.ExecuteNonQuery();
    }

    public HeadRow? GetHeadByCompanyId(long companyId)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Auth_GetHeadByCompanyID", ("@CompanyID", companyId));
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new HeadRow
        {
            CompanyID   = Db.L(r, "CompanyID"),
            CompanyName = Db.S(r, "CompanyName"),
            Mobile      = Db.S(r, "Mobile"),
            Password    = Db.S(r, "Password"),
            BranchID    = Db.L(r, "BranchID"),
            IsActive    = Db.B(r, "IsActive")
        };
    }

    public StaffRow? GetStaffById(long employeeId)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Auth_GetStaffByID", ("@EmployeeID", employeeId));
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new StaffRow
        {
            EmployeeID = Db.L(r, "EmployeeID"),
            CompanyID  = Db.L(r, "CompanyID"),
            BranchID   = Db.L(r, "BranchID"),
            FullName   = Db.S(r, "FullName"),
            Email      = Db.S(r, "Email"),
            Password   = Db.S(r, "Password"),
            IsActive   = Db.B(r, "IsActive"),
            RoleID     = Db.L(r, "RoleID"),
            RoleName   = Db.S(r, "RoleName")
        };
    }

    /// <summary>0300-1234567 / 03001234567 / +923001234567 → 0300-1234567 canonical.</summary>
    public static string NormalizeMobile(string raw)
    {
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("92")) digits = "0" + digits[2..];
        return digits.Length == 11 ? $"{digits[..4]}-{digits[4..]}" : raw.Trim();
    }

    public void UpdateHeadPassword(long companyId, string passwordHash)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Auth_UpdateHeadPassword",
            ("@CompanyID", companyId), ("@PasswordHash", passwordHash));
        cmd.ExecuteNonQuery();
    }

    public void UpdateStaffPassword(long companyId, long employeeId, string passwordHash)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Auth_UpdateStaffPassword",
            ("@CompanyID", companyId), ("@EmployeeID", employeeId), ("@PasswordHash", passwordHash));
        cmd.ExecuteNonQuery();
    }
}
