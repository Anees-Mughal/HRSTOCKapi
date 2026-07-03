using System.Security.Claims;

namespace HRStackPK.Api.Helpers;

/// <summary>Tenant identity comes from JWT claims ONLY — never from the client.</summary>
public static class ClaimsX
{
    public static long CompanyId(this ClaimsPrincipal u) =>
        long.Parse(u.FindFirst("CompanyID")?.Value ?? "0");

    public static long BranchId(this ClaimsPrincipal u) =>
        long.Parse(u.FindFirst("BranchID")?.Value ?? "0");

    /// <summary>0 for Head login (Head account lives in Companies, not Employees).</summary>
    public static long EmployeeId(this ClaimsPrincipal u) =>
        long.Parse(u.FindFirst("EmployeeID")?.Value ?? "0");

    /// <summary>EmployeeID as nullable — NULL for Head (FK-safe for MarkedBy/CreatedBy columns).</summary>
    public static long? EmployeeIdOrNull(this ClaimsPrincipal u)
    {
        var id = u.EmployeeId();
        return id == 0 ? null : id;
    }

    public static string Role(this ClaimsPrincipal u) =>
        u.FindFirst("Role")?.Value ?? "";

    public static string LoginType(this ClaimsPrincipal u) =>
        u.FindFirst("LoginType")?.Value ?? "";

    public static bool IsHead(this ClaimsPrincipal u) => u.LoginType() == "Head";
}

public static class MonthX
{
    /// <summary>"2026-07" → (7, 2026); null/invalid → current month.</summary>
    public static (int month, int year) Parse(string? monthStr)
    {
        if (!string.IsNullOrWhiteSpace(monthStr))
        {
            var parts = monthStr.Split('-');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var y) &&
                int.TryParse(parts[1], out var m) &&
                m is >= 1 and <= 12)
                return (m, y);
        }
        var now = DateTime.Today;
        return (now.Month, now.Year);
    }

    public static string Format(int month, int year) => $"{year}-{month:00}";
}
