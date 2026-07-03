using HRStackPK.Api.Models;
using Microsoft.Data.SqlClient;

namespace HRStackPK.Api.DAL;

public class AttendanceDAL
{
    private readonly Db _db;
    public AttendanceDAL(Db db) => _db = db;

    private static AttendanceDto Map(SqlDataReader r) => new()
    {
        Id         = Db.L(r, "ID"),
        EmployeeId = Db.L(r, "EmployeeID"),
        Date       = Db.DateS(r, "Date"),
        Status     = Db.S(r, "Status"),
        InTime     = Db.TimeS(r, "TimeIn"),
        OutTime    = Db.TimeS(r, "TimeOut")
    };

    public List<AttendanceDto> ByDate(long companyId, long branchId, string date)
    {
        var list = new List<AttendanceDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Attendance_ByDate",
            ("@CompanyID", companyId), ("@BranchID", branchId), ("@Date", date));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public AttendanceDto Mark(long companyId, long branchId, MarkAttendanceRequest req, long? markedById)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Attendance_Mark",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@EmployeeID", req.EmployeeId), ("@Date", req.Date),
            ("@Status", req.Status),
            ("@TimeIn",  string.IsNullOrWhiteSpace(req.InTime)  ? null : req.InTime),
            ("@TimeOut", string.IsNullOrWhiteSpace(req.OutTime) ? null : req.OutTime),
            ("@MarkedByID", markedById));
        using var r = cmd.ExecuteReader();
        r.Read();
        return Map(r);
    }

    public List<AttendanceDto> History(long companyId, long employeeId, string? from, string? to)
    {
        var list = new List<AttendanceDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Attendance_History",
            ("@CompanyID", companyId), ("@EmployeeID", employeeId),
            ("@From", string.IsNullOrWhiteSpace(from) ? null : from),
            ("@To",   string.IsNullOrWhiteSpace(to)   ? null : to));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }
}

public class LeavesDAL
{
    private readonly Db _db;
    public LeavesDAL(Db db) => _db = db;

    private static LeaveDto Map(SqlDataReader r) => new()
    {
        Id           = Db.L(r, "ID"),
        EmployeeId   = Db.L(r, "EmployeeID"),
        EmployeeName = Db.S(r, "EmployeeName"),
        TypeId       = Db.L(r, "LeaveTypeID"),
        Type         = Db.S(r, "TypeName"),
        From         = Db.DateS(r, "FromDate"),
        To           = Db.DateS(r, "ToDate"),
        Days         = Db.I(r, "TotalDays"),
        Reason       = Db.S(r, "Reason"),
        Status       = Db.S(r, "Status"),
        AppliedOn    = Db.DateS(r, "CreatedAt")
    };

    public List<LeaveTypeDto> Types(long companyId)
    {
        var list = new List<LeaveTypeDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_LeaveTypes_List", ("@CompanyID", companyId));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new LeaveTypeDto
        {
            Id = Db.L(r, "ID"), Name = Db.S(r, "Name"), TotalDays = Db.I(r, "TotalDays")
        });
        return list;
    }

    public List<LeaveDto> List(long companyId, long branchId)
    {
        var list = new List<LeaveDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Leaves_List",
            ("@CompanyID", companyId), ("@BranchID", branchId));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public List<LeaveDto> Me(long companyId, long employeeId)
    {
        var list = new List<LeaveDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Leaves_Me",
            ("@CompanyID", companyId), ("@EmployeeID", employeeId));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public LeaveDto Apply(long companyId, long branchId, long employeeId, ApplyLeaveRequest req)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Leaves_Apply",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@EmployeeID", employeeId), ("@LeaveTypeID", req.TypeId),
            ("@FromDate", req.From), ("@ToDate", req.To),
            ("@TotalDays", req.Days), ("@Reason", req.Reason));
        using var r = cmd.ExecuteReader();
        r.Read();
        return Map(r);
    }

    public LeaveDto? SetStatus(long companyId, long leaveId, string status, long? approvedById, string? rejectionReason)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Leaves_SetStatus",
            ("@CompanyID", companyId), ("@LeaveID", leaveId),
            ("@Status", status), ("@ApprovedByID", approvedById),
            ("@RejectionReason", rejectionReason));
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public List<LeaveBalanceDto> BalancesMe(long companyId, long branchId, long employeeId, int year)
    {
        var list = new List<LeaveBalanceDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_LeaveBalances_Me",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@EmployeeID", employeeId), ("@Year", year));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new LeaveBalanceDto
        {
            Type      = Db.S(r, "Type"),
            Total     = Db.I(r, "TotalDays"),
            Used      = Db.I(r, "UsedDays"),
            Remaining = Db.I(r, "RemainingDays")
        });
        return list;
    }
}

public class LoansDAL
{
    private readonly Db _db;
    public LoansDAL(Db db) => _db = db;

    private static LoanDto Map(SqlDataReader r) => new()
    {
        Id           = Db.L(r, "ID"),
        EmployeeId   = Db.L(r, "EmployeeID"),
        EmployeeName = Db.S(r, "EmployeeName"),
        LoanType     = Db.S(r, "LoanType"),
        Amount       = Db.Dec(r, "TotalAmount"),
        Instalment   = Db.Dec(r, "MonthlyDeduction"),
        Remaining    = Db.Dec(r, "RemainingAmount"),
        Status       = Db.S(r, "Status"),
        Reason       = Db.S(r, "Reason"),
        Date         = Db.DateS(r, "AppliedDate")
    };

    public List<LoanDto> List(long companyId, long branchId)
    {
        var list = new List<LoanDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Loans_List",
            ("@CompanyID", companyId), ("@BranchID", branchId));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public List<LoanDto> Me(long companyId, long employeeId)
    {
        var list = new List<LoanDto>();
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Loans_Me",
            ("@CompanyID", companyId), ("@EmployeeID", employeeId));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public LoanDto Apply(long companyId, long branchId, long employeeId, ApplyLoanRequest req)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Loans_Apply",
            ("@CompanyID", companyId), ("@BranchID", branchId),
            ("@EmployeeID", employeeId),
            ("@LoanType", string.IsNullOrWhiteSpace(req.LoanType) ? "Loan" : req.LoanType),
            ("@Amount", req.Amount), ("@Instalment", req.Instalment),
            ("@Reason", req.Reason));
        using var r = cmd.ExecuteReader();
        r.Read();
        return Map(r);
    }

    public LoanDto? SetStatus(long companyId, long loanId, string status, long? approvedById, string? rejectionReason)
    {
        using var con = _db.Open();
        using var cmd = Db.Sp(con, "usp_Loans_SetStatus",
            ("@CompanyID", companyId), ("@LoanID", loanId),
            ("@Status", status), ("@ApprovedByID", approvedById),
            ("@RejectionReason", rejectionReason));
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }
}
