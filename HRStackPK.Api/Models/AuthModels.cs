namespace HRStackPK.Api.Models;

// ---------- Requests ----------
public class SignupRequest
{
    public string CompanyName { get; set; } = "";
    public string OwnerName  { get; set; } = "";
    public string Phone      { get; set; } = "";   // Head login identifier (03xx-xxxxxxx)
    public string Email      { get; set; } = "";
    public string Password   { get; set; } = "";
}

public class LoginRequest
{
    public string Identifier { get; set; } = "";   // mobile (Head) or email (Staff)
    public string Password   { get; set; } = "";
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = "";
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class ResetPasswordRequest
{
    public string? NewPassword { get; set; }
}
// ---------- Responses ----------
public class AuthUser
{
    public long   Id         { get; set; }         // CompanyID for Head, EmployeeID for Staff
    public string Identifier { get; set; } = "";
    public string Role       { get; set; } = "";
    public long   RoleId     { get; set; }
    public long   TenantId   { get; set; }         // CompanyID
    public long   BranchId   { get; set; }
    public string FullName   { get; set; } = "";
    public long?  EmployeeId { get; set; }         // null for Head
}

public class AuthResponse
{
    public string   AccessToken  { get; set; } = "";
    public string   RefreshToken { get; set; } = "";
    public AuthUser User         { get; set; } = new();
}

// ---------- Internal DAL rows ----------
public class HeadRow
{
    public long   CompanyID   { get; set; }
    public string CompanyName { get; set; } = "";
    public string Mobile      { get; set; } = "";
    public string Password    { get; set; } = "";  // BCrypt hash
    public long   BranchID    { get; set; }        // first/main branch
    public bool   IsActive    { get; set; }
}

public class StaffRow
{
    public long   EmployeeID { get; set; }
    public long   CompanyID  { get; set; }
    public long   BranchID   { get; set; }
    public string FullName   { get; set; } = "";
    public string Email      { get; set; } = "";
    public string Password   { get; set; } = "";   // BCrypt hash
    public bool   IsActive   { get; set; }
    public long   RoleID     { get; set; }
    public string RoleName   { get; set; } = "";
}

public class RefreshTokenRow
{
    public long     ID          { get; set; }
    public long     CompanyID   { get; set; }
    public long?    EmployeeID  { get; set; }
    public bool     IsHeadLogin { get; set; }
    public string   Token       { get; set; } = "";
    public DateTime ExpiresAt   { get; set; }
    public bool     IsRevoked   { get; set; }
}
