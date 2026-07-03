namespace HRStackPK.Api.Models;

// =====================================================================
// EMPLOYEES  — matches frontend {id, fullName, cnic, departmentId, ...}
// =====================================================================
public class EmployeeDto
{
    public long    Id             { get; set; }
    public string  FullName       { get; set; } = "";
    public string  Cnic           { get; set; } = "";
    public long?   DepartmentId   { get; set; }
    public string  Designation    { get; set; } = "";
    public string  EmploymentType { get; set; } = "";
    public string  JoiningDate    { get; set; } = "";
    public decimal BasicSalary    { get; set; }
    public decimal Conveyance     { get; set; }
    public string  BloodGroup     { get; set; } = "";
    public string  Phone          { get; set; } = "";
    public string  Email          { get; set; } = "";
    public bool    IsActive       { get; set; }
}

public class EmployeeUpsertRequest
{
    public string  FullName       { get; set; } = "";
    public string? Cnic           { get; set; }
    public long?   DepartmentId   { get; set; }
    public string? Designation    { get; set; }
    public string? EmploymentType { get; set; }
    public string? JoiningDate    { get; set; }     // yyyy-MM-dd
    public decimal BasicSalary    { get; set; }
    public decimal Conveyance     { get; set; }
    public string? BloodGroup     { get; set; }
    public string? Phone          { get; set; }
    public string  Email          { get; set; } = "";
    public string? Password       { get; set; }     // create only; default "123456"
    public bool    IsActive       { get; set; } = true;
}

// =====================================================================
// DEPARTMENTS
// =====================================================================
public class DepartmentDto
{
    public long   Id        { get; set; }
    public string Name      { get; set; } = "";
    public long?  ManagerId { get; set; }
}

public class DepartmentUpsertRequest
{
    public string Name      { get; set; } = "";
    public long?  ManagerId { get; set; }
}

// =====================================================================
// ATTENDANCE
// =====================================================================
public class AttendanceDto
{
    public long    Id         { get; set; }
    public long    EmployeeId { get; set; }
    public string  Date       { get; set; } = "";
    public string  Status     { get; set; } = "";
    public string? InTime     { get; set; }
    public string? OutTime    { get; set; }
}

public class MarkAttendanceRequest
{
    public long    EmployeeId { get; set; }
    public string  Date       { get; set; } = "";   // yyyy-MM-dd
    public string  Status     { get; set; } = "";   // Present / Late / Absent
    public string? InTime     { get; set; }         // "09:05"
    public string? OutTime    { get; set; }
}

// =====================================================================
// LEAVES
// =====================================================================
public class LeaveDto
{
    public long    Id           { get; set; }
    public long    EmployeeId   { get; set; }
    public string  EmployeeName { get; set; } = "";
    public long    TypeId       { get; set; }
    public string  Type         { get; set; } = "";
    public string  From         { get; set; } = "";
    public string  To           { get; set; } = "";
    public int     Days         { get; set; }
    public string  Reason       { get; set; } = "";
    public string  Status       { get; set; } = "";
    public string  AppliedOn    { get; set; } = "";
}

public class ApplyLeaveRequest
{
    public long?   EmployeeId { get; set; }         // omitted → self (from JWT)
    public long    TypeId     { get; set; }
    public string  From       { get; set; } = "";
    public string  To         { get; set; } = "";
    public int     Days       { get; set; }
    public string? Reason     { get; set; }
}

public class StatusRequest
{
    public string  Status          { get; set; } = "";   // Approved / Rejected
    public string? RejectionReason { get; set; }
}

public class LeaveBalanceDto
{
    public string Type      { get; set; } = "";
    public int    Total     { get; set; }
    public int    Used      { get; set; }
    public int    Remaining { get; set; }
}

public class LeaveTypeDto
{
    public long   Id        { get; set; }
    public string Name      { get; set; } = "";
    public int    TotalDays { get; set; }
}

// =====================================================================
// LOANS
// =====================================================================
public class LoanDto
{
    public long    Id           { get; set; }
    public long    EmployeeId   { get; set; }
    public string  EmployeeName { get; set; } = "";
    public string  LoanType     { get; set; } = "";
    public decimal Amount       { get; set; }
    public decimal Instalment   { get; set; }
    public decimal Remaining    { get; set; }
    public string  Status       { get; set; } = "";
    public string  Reason       { get; set; } = "";
    public string  Date         { get; set; } = "";
}

public class ApplyLoanRequest
{
    public long?    EmployeeId { get; set; }        // omitted → self (from JWT)
    public string?  LoanType   { get; set; }        // Loan / Advance
    public decimal  Amount     { get; set; }
    public decimal  Instalment { get; set; }
    public string?  Reason     { get; set; }
}

// =====================================================================
// PAYROLL
// =====================================================================
public class OverrideFields
{
    public decimal? Basic         { get; set; }
    public decimal? Hra           { get; set; }
    public decimal? Medical       { get; set; }
    public decimal? Conveyance    { get; set; }
    public decimal? Tax           { get; set; }
    public decimal? Eobi          { get; set; }
    public decimal? Loan          { get; set; }
    public decimal? LateDeduction { get; set; }

    public bool IsEmpty() =>
        Basic is null && Hra is null && Medical is null && Conveyance is null &&
        Tax is null && Eobi is null && Loan is null && LateDeduction is null;
}

public class OverridePutRequest : OverrideFields
{
    public string? Month { get; set; }              // "2026-07"; default current
}

public class PayrollRow
{
    public long    EmployeeId      { get; set; }
    public decimal Basic           { get; set; }
    public decimal Hra             { get; set; }
    public decimal Medical         { get; set; }
    public decimal Conveyance      { get; set; }
    public decimal Gross           { get; set; }
    public decimal Tax             { get; set; }
    public decimal Eobi            { get; set; }
    public decimal Loan            { get; set; }
    public decimal LateDeduction   { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal Net             { get; set; }
    public int     WorkingDays     { get; set; }
    public int     PresentDays     { get; set; }
    public int     AbsentDays      { get; set; }
    public int     LateDays        { get; set; }
    public bool    IsOverridden    { get; set; }
}

public class PayrollGenerateRequest
{
    public string?          Month { get; set; }     // "2026-07"; default current
    public List<PayrollRow> Rows  { get; set; } = new();
}

// =====================================================================
// PAYSLIPS
// =====================================================================
public class PayslipDto
{
    public long    Id              { get; set; }
    public long    EmployeeId      { get; set; }
    public string  EmployeeName    { get; set; } = "";
    public string  Month           { get; set; } = "";  // "2026-07"
    public decimal Basic           { get; set; }
    public decimal Hra             { get; set; }
    public decimal Medical         { get; set; }
    public decimal Conveyance      { get; set; }
    public decimal Gross           { get; set; }
    public decimal Tax             { get; set; }
    public decimal Eobi            { get; set; }
    public decimal Loan            { get; set; }
    public decimal LateDeduction   { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal Net             { get; set; }
    public string  GeneratedOn     { get; set; } = "";
}

public class PayslipOverviewDto
{
    public long     EmployeeId { get; set; }
    public string   FullName   { get; set; } = "";
    public bool     Generated  { get; set; }
    public decimal? Net        { get; set; }
}

// =====================================================================
// ACCOUNTS
// =====================================================================
public class ExpenseDto
{
    public long    Id          { get; set; }
    public string  Date        { get; set; } = "";
    public string  Category    { get; set; } = "";
    public string  Description { get; set; } = "";
    public decimal Amount      { get; set; }
    public bool    IsSalary    { get; set; }            // derived from generated payslip
}

public class RevenueDto
{
    public long    Id          { get; set; }
    public string  Date        { get; set; } = "";
    public string  Source      { get; set; } = "";
    public string  Description { get; set; } = "";
    public decimal Amount      { get; set; }
}

public class CreateExpenseRequest
{
    public string? Date        { get; set; }
    public string  Category    { get; set; } = "";
    public string? Description { get; set; }
    public decimal Amount      { get; set; }
}

public class CreateRevenueRequest
{
    public string? Date        { get; set; }
    public string  Source      { get; set; } = "";
    public string? Description { get; set; }
    public decimal Amount      { get; set; }
}

public class ReportGroup
{
    public string  Head   { get; set; } = "";
    public decimal Amount { get; set; }
}

public class ReportSection
{
    public decimal            Total  { get; set; }
    public List<ReportGroup>  Groups { get; set; } = new();
}

public class ProfitLoss
{
    public decimal Revenue  { get; set; }
    public decimal Expenses { get; set; }
    public decimal Net      { get; set; }
}

public class AccountsReportDto
{
    public string        Month       { get; set; } = "";
    public ReportSection Expenses    { get; set; } = new();
    public ReportSection Revenue     { get; set; } = new();
    public ProfitLoss    Pl          { get; set; } = new();
    public int           SalaryCount { get; set; }
}

// =====================================================================
// APPRAISALS
// =====================================================================
public class AppraisalDto
{
    public long    Id             { get; set; }
    public long    EmployeeId     { get; set; }
    public string  EmployeeName   { get; set; } = "";
    public string  Cycle          { get; set; } = "";
    public string  Goals          { get; set; } = "";
    public int?    Rating         { get; set; }
    public string  ManagerComment { get; set; } = "";
    public string  Status         { get; set; } = "";
}

public class AppraisalCreateRequest
{
    public long    EmployeeId     { get; set; }
    public string  Cycle          { get; set; } = "";
    public string? Goals          { get; set; }
    public int?    Rating         { get; set; }
    public string? ManagerComment { get; set; }
    public string? Status         { get; set; }
}
