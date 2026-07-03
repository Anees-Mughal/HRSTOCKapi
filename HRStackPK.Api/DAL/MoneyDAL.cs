using System.Text.Json;
using HRStackPK.Api.Helpers;
using HRStackPK.Api.Models;
using Microsoft.Data.SqlClient;

namespace HRStackPK.Api.DAL;

public class PayrollDAL
{
    private static readonly JsonSerializerOptions CamelJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Db _db;
    public PayrollDAL(Db db) => _db = db;

    public Dictionary<string, OverrideFields> GetOverrides(long companyId, long branchId, int month, int year)
    {
        var dict = new Dictionary<string, OverrideFields>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_PayrollOverrides_Get",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@Month", month), ("@Year", year));
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            dict[Db.L(r, "EmployeeID").ToString()] = new OverrideFields
            {
                Basic         = Db.DecN(r, "Basic"),
                Hra           = Db.DecN(r, "HRA"),
                Medical       = Db.DecN(r, "Medical"),
                Conveyance    = Db.DecN(r, "Conveyance"),
                Tax           = Db.DecN(r, "Tax"),
                Eobi          = Db.DecN(r, "EOBI"),
                Loan          = Db.DecN(r, "Loan"),
                LateDeduction = Db.DecN(r, "LateDeduction")
            };
        }
        return dict;
    }

    public void UpsertOverride(long companyId, long branchId, long employeeId, int month, int year, OverrideFields f)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_PayrollOverrides_Upsert",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@EmployeeID", employeeId), ("@Month", month), ("@Year", year),
            ("@Basic", f.Basic), ("@HRA", f.Hra), ("@Medical", f.Medical),
            ("@Conveyance", f.Conveyance), ("@Tax", f.Tax), ("@EOBI", f.Eobi),
            ("@Loan", f.Loan), ("@LateDeduction", f.LateDeduction));
        cmd.ExecuteNonQuery();
    }

    public void ResetOverride(long companyId, long employeeId, int month, int year)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_PayrollOverrides_Reset",
            ("@CompanyID", companyId), ("@EmployeeID", employeeId),
            ("@Month", month), ("@Year", year));
        cmd.ExecuteNonQuery();
    }

    /// <summary>Throws SqlException 51002 if already generated for the month.</summary>
    public (long runId, int slipCount) Generate(long companyId, long branchId, int month, int year,
                                                long? generatedById, List<PayrollRow> rows)
    {
        var json = JsonSerializer.Serialize(rows, CamelJson);
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Payroll_Generate",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@Month", month), ("@Year", year),
            ("@GeneratedByID", generatedById), ("@RowsJson", json));
        using var r = cmd.ExecuteReader();
        r.Read();
        return (Db.L(r, "PayrollRunID"), Db.I(r, "SlipCount"));
    }
}

public class PayslipsDAL
{
    private readonly Db _db;
    public PayslipsDAL(Db db) => _db = db;

    private static PayslipDto Map(SqlDataReader r)
    {
        var dto = new PayslipDto
        {
            Id            = Db.L(r, "ID"),
            EmployeeId    = Db.L(r, "EmployeeID"),
            EmployeeName  = Db.S(r, "EmployeeName"),
            Month         = MonthX.Format(Db.I(r, "Month"), Db.I(r, "Year")),
            Basic         = Db.Dec(r, "BasicSalary"),
            Hra           = Db.Dec(r, "HRA"),
            Medical       = Db.Dec(r, "Medical"),
            Conveyance    = Db.Dec(r, "Conveyance"),
            Gross         = Db.Dec(r, "GrossSalary"),
            Tax           = Db.Dec(r, "IncomeTax"),
            Eobi          = Db.Dec(r, "EOBI"),
            Loan          = Db.Dec(r, "LoanDeduction"),
            LateDeduction = Db.Dec(r, "LateDeduction"),
            Net           = Db.Dec(r, "NetSalary"),
            GeneratedOn   = Db.DateS(r, "GeneratedAt")
        };
        dto.TotalDeductions = dto.Tax + dto.Eobi + dto.Loan + dto.LateDeduction;
        return dto;
    }

    public List<PayslipDto> List(long companyId, long branchId)
    {
        var list = new List<PayslipDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Payslips_List",
            ("@CompanyID", companyId), ("@BranchID", branchId));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public List<PayslipDto> ByEmployee(long companyId, long employeeId)
    {
        var list = new List<PayslipDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Payslips_ByEmployee",
            ("@CompanyID", companyId), ("@EmployeeID", employeeId));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public List<PayslipOverviewDto> Overview(long companyId, long branchId, int month, int year)
    {
        var list = new List<PayslipOverviewDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Payslips_Overview",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@Month", month), ("@Year", year));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new PayslipOverviewDto
        {
            EmployeeId = Db.L(r, "EmployeeID"),
            FullName   = Db.S(r, "FullName"),
            Generated  = Db.B(r, "Generated"),
            Net        = Db.DecN(r, "NetSalary")
        });
        return list;
    }
}

public class AccountsDAL
{
    private readonly Db _db;
    public AccountsDAL(Db db) => _db = db;

    public List<ExpenseDto> Expenses(long companyId, long branchId, int? month, int? year)
    {
        var list = new List<ExpenseDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Transactions_List",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@Type", "Expense"), ("@Month", month), ("@Year", year));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new ExpenseDto
        {
            Id          = Db.L(r, "ID"),
            Date        = Db.DateS(r, "TransactionDate"),
            Category    = Db.S(r, "Category"),
            Description = Db.S(r, "Description"),
            Amount      = Db.Dec(r, "Amount"),
            IsSalary    = Db.Ln(r, "PayslipID") != null
        });
        return list;
    }

    public List<RevenueDto> Revenues(long companyId, long branchId, int? month, int? year)
    {
        var list = new List<RevenueDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Transactions_List",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@Type", "Revenue"), ("@Month", month), ("@Year", year));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new RevenueDto
        {
            Id          = Db.L(r, "ID"),
            Date        = Db.DateS(r, "TransactionDate"),
            Source      = Db.S(r, "Category"),
            Description = Db.S(r, "Description"),
            Amount      = Db.Dec(r, "Amount")
        });
        return list;
    }

    public ExpenseDto CreateExpense(long companyId, long branchId, CreateExpenseRequest req, long? createdById)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Transactions_Create",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@Type", "Expense"), ("@Category", req.Category.Trim()),
            ("@Description", req.Description),
            ("@Amount", req.Amount),
            ("@TransactionDate", string.IsNullOrWhiteSpace(req.Date)
                ? DateTime.Today.ToString("yyyy-MM-dd") : req.Date),
            ("@CreatedByID", createdById));
        using var r = cmd.ExecuteReader();
        r.Read();
        return new ExpenseDto
        {
            Id          = Db.L(r, "ID"),
            Date        = Db.DateS(r, "TransactionDate"),
            Category    = Db.S(r, "Category"),
            Description = Db.S(r, "Description"),
            Amount      = Db.Dec(r, "Amount"),
            IsSalary    = false
        };
    }

    public RevenueDto CreateRevenue(long companyId, long branchId, CreateRevenueRequest req, long? createdById)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Transactions_Create",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@Type", "Revenue"), ("@Category", req.Source.Trim()),
            ("@Description", req.Description),
            ("@Amount", req.Amount),
            ("@TransactionDate", string.IsNullOrWhiteSpace(req.Date)
                ? DateTime.Today.ToString("yyyy-MM-dd") : req.Date),
            ("@CreatedByID", createdById));
        using var r = cmd.ExecuteReader();
        r.Read();
        return new RevenueDto
        {
            Id          = Db.L(r, "ID"),
            Date        = Db.DateS(r, "TransactionDate"),
            Source      = Db.S(r, "Category"),
            Description = Db.S(r, "Description"),
            Amount      = Db.Dec(r, "Amount")
        };
    }

    public AccountsReportDto Report(long companyId, long branchId, int month, int year)
    {
        var report = new AccountsReportDto { Month = MonthX.Format(month, year) };

        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Accounts_Report",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@Month", month), ("@Year", year));
        using var r = cmd.ExecuteReader();

        // 1) expense groups
        while (r.Read())
            report.Expenses.Groups.Add(new ReportGroup
            {
                Head = Db.S(r, "Head"), Amount = Db.Dec(r, "Amount")
            });
        report.Expenses.Total = report.Expenses.Groups.Sum(g => g.Amount);

        // 2) revenue groups
        r.NextResult();
        while (r.Read())
            report.Revenue.Groups.Add(new ReportGroup
            {
                Head = Db.S(r, "Head"), Amount = Db.Dec(r, "Amount")
            });
        report.Revenue.Total = report.Revenue.Groups.Sum(g => g.Amount);

        // 3) salary slip count
        r.NextResult();
        if (r.Read()) report.SalaryCount = Db.I(r, "SalaryCount");

        report.Pl = new ProfitLoss
        {
            Revenue  = report.Revenue.Total,
            Expenses = report.Expenses.Total,
            Net      = report.Revenue.Total - report.Expenses.Total
        };
        return report;
    }
}

public class AppraisalsDAL
{
    private readonly Db _db;
    public AppraisalsDAL(Db db) => _db = db;

    private static AppraisalDto Map(SqlDataReader r) => new()
    {
        Id             = Db.L(r, "ID"),
        EmployeeId     = Db.L(r, "EmployeeID"),
        EmployeeName   = Db.S(r, "EmployeeName"),
        Cycle          = Db.S(r, "Cycle"),
        Goals          = Db.S(r, "Goals"),
        Rating         = Db.In(r, "Rating"),
        ManagerComment = Db.S(r, "ManagerComment"),
        Status         = Db.S(r, "Status")
    };

    public List<AppraisalDto> List(long companyId, long branchId)
    {
        var list = new List<AppraisalDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Appraisals_List",
            ("@CompanyID", companyId), ("@BranchID", branchId));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public AppraisalDto Create(long companyId, long branchId, AppraisalCreateRequest req)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Appraisals_Create",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@EmployeeID", req.EmployeeId), ("@Cycle", req.Cycle.Trim()),
            ("@Goals", req.Goals), ("@Rating", req.Rating),
            ("@ManagerComment", req.ManagerComment),
            ("@Status", string.IsNullOrWhiteSpace(req.Status) ? "Draft" : req.Status));
        using var r = cmd.ExecuteReader();
        r.Read();
        return Map(r);
    }
}
