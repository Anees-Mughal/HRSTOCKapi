using HRStackPK.Api.Models;
using Microsoft.Data.SqlClient;

namespace HRStackPK.Api.DAL;

public class EmployeesDAL
{
    private readonly Db _db;
    public EmployeesDAL(Db db) => _db = db;

    private static EmployeeDto Map(SqlDataReader r) => new()
    {
        Id             = Db.L(r, "ID"),
        FullName       = Db.S(r, "FullName"),
        Cnic           = Db.S(r, "CNIC"),
        DepartmentId   = Db.Ln(r, "DepartmentID"),
        Designation    = Db.S(r, "Designation"),
        EmploymentType = Db.S(r, "EmployeeType"),
        JoiningDate    = Db.DateS(r, "JoiningDate"),
        BasicSalary    = Db.Dec(r, "BasicSalary"),
        Conveyance     = Db.Dec(r, "Conveyance"),
        BloodGroup     = Db.S(r, "BloodGroup"),
        Phone          = Db.S(r, "Mobile"),
        Email          = Db.S(r, "Email"),
        IsActive       = Db.B(r, "IsActive")
    };

    public List<EmployeeDto> List(long companyId, long branchId)
    {
        var list = new List<EmployeeDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Employees_List",
            ("@CompanyID", companyId), ("@BranchID", branchId));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public EmployeeDto? GetById(long companyId, long employeeId)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Employees_GetByID",
            ("@CompanyID", companyId), ("@EmployeeID", employeeId));
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public EmployeeDto Create(long companyId, long branchId, EmployeeUpsertRequest req, string passwordHash)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Employees_Create",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@FullName", req.FullName.Trim()),
            ("@CNIC", Empty(req.Cnic)),
            ("@DepartmentID", req.DepartmentId),
            ("@Designation", Empty(req.Designation)),
            ("@EmployeeType", Empty(req.EmploymentType)),
            ("@JoiningDate", Empty(req.JoiningDate)),
            ("@BasicSalary", req.BasicSalary),
            ("@Conveyance", req.Conveyance),
            ("@BloodGroup", Empty(req.BloodGroup)),
            ("@Mobile", Empty(req.Phone)),
            ("@Email", req.Email.Trim()),
            ("@PasswordHash", passwordHash));
        using var r = cmd.ExecuteReader();
        r.Read();
        return Map(r);
    }

    public EmployeeDto? Update(long companyId, long employeeId, EmployeeUpsertRequest req)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Employees_Update",
            ("@CompanyID", companyId), ("@EmployeeID", employeeId),
            ("@FullName", req.FullName.Trim()),
            ("@CNIC", Empty(req.Cnic)),
            ("@DepartmentID", req.DepartmentId),
            ("@Designation", Empty(req.Designation)),
            ("@EmployeeType", Empty(req.EmploymentType)),
            ("@JoiningDate", Empty(req.JoiningDate)),
            ("@BasicSalary", req.BasicSalary),
            ("@Conveyance", req.Conveyance),
            ("@BloodGroup", Empty(req.BloodGroup)),
            ("@Mobile", Empty(req.Phone)),
            ("@Email", req.Email.Trim()),
            ("@IsActive", req.IsActive));
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public void Delete(long companyId, long employeeId)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Employees_Delete",
            ("@CompanyID", companyId), ("@EmployeeID", employeeId));
        cmd.ExecuteNonQuery();
    }

    private static object? Empty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

public class DepartmentsDAL
{
    private readonly Db _db;
    public DepartmentsDAL(Db db) => _db = db;

    private static DepartmentDto Map(SqlDataReader r) => new()
    {
        Id        = Db.L(r, "ID"),
        Name      = Db.S(r, "Name"),
        ManagerId = Db.Ln(r, "ManagerID")
    };

    public List<DepartmentDto> List(long companyId, long branchId)
    {
        var list = new List<DepartmentDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Departments_List",
            ("@CompanyID", companyId), ("@BranchID", branchId));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public DepartmentDto Create(long companyId, long branchId, DepartmentUpsertRequest req)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Departments_Create",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@Name", req.Name.Trim()), ("@ManagerID", req.ManagerId));
        using var r = cmd.ExecuteReader();
        r.Read();
        return Map(r);
    }

    public DepartmentDto? Update(long companyId, long departmentId, DepartmentUpsertRequest req)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Departments_Update",
            ("@CompanyID", companyId), ("@DepartmentID", departmentId),
            ("@Name", req.Name.Trim()), ("@ManagerID", req.ManagerId));
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }
}
