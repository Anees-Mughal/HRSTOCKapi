-- =====================================================================
-- HRStackPK — Stored Procedures: ALL MODULES
-- Run AFTER: 1_HRStackPK_Database.sql → 2_..._Update_v2.sql → 3_..._SP_Auth.sql
-- =====================================================================
USE HRStackPK;
GO

-- =====================================================================
-- EMPLOYEES
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Employees_List
    @CompanyID BIGINT, @BranchID BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, FullName, CNIC, DepartmentID, Designation, EmployeeType, JoiningDate,
           BasicSalary, Conveyance, BloodGroup, Mobile, Email, IsActive
    FROM Employees
    WHERE CompanyID = @CompanyID AND BranchID = @BranchID
    ORDER BY ID;
END
GO

CREATE OR ALTER PROCEDURE usp_Employees_GetByID
    @CompanyID BIGINT, @EmployeeID BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, FullName, CNIC, DepartmentID, Designation, EmployeeType, JoiningDate,
           BasicSalary, Conveyance, BloodGroup, Mobile, Email, IsActive
    FROM Employees
    WHERE CompanyID = @CompanyID AND ID = @EmployeeID;
END
GO

CREATE OR ALTER PROCEDURE usp_Employees_Create
    @CompanyID BIGINT, @BranchID BIGINT,
    @FullName NVARCHAR(200), @CNIC NVARCHAR(20) = NULL, @DepartmentID BIGINT = NULL,
    @Designation NVARCHAR(150) = NULL, @EmployeeType NVARCHAR(50) = NULL,
    @JoiningDate DATE = NULL, @BasicSalary DECIMAL(12,2) = 0, @Conveyance DECIMAL(12,2) = 0,
    @BloodGroup NVARCHAR(10) = NULL, @Mobile NVARCHAR(20) = NULL,
    @Email NVARCHAR(150), @PasswordHash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM Employees WHERE Email = @Email)
        THROW 51003, 'Email already registered', 1;

    INSERT INTO Employees (CompanyID, BranchID, DepartmentID, FullName, Email, Mobile, CNIC,
                           Designation, EmployeeType, JoiningDate, BasicSalary, Conveyance,
                           BloodGroup, Password)
    VALUES (@CompanyID, @BranchID, @DepartmentID, @FullName, @Email, @Mobile, @CNIC,
            @Designation, @EmployeeType, @JoiningDate, @BasicSalary, @Conveyance,
            @BloodGroup, @PasswordHash);

    DECLARE @ID BIGINT = SCOPE_IDENTITY();

    -- Assign default Staff role (system RoleID 4)
    INSERT INTO EmployeeRoles (CompanyID, BranchID, EmployeeID, RoleID)
    VALUES (@CompanyID, @BranchID, @ID, 4);

    EXEC usp_Employees_GetByID @CompanyID, @ID;
END
GO

CREATE OR ALTER PROCEDURE usp_Employees_Update
    @CompanyID BIGINT, @EmployeeID BIGINT,
    @FullName NVARCHAR(200), @CNIC NVARCHAR(20) = NULL, @DepartmentID BIGINT = NULL,
    @Designation NVARCHAR(150) = NULL, @EmployeeType NVARCHAR(50) = NULL,
    @JoiningDate DATE = NULL, @BasicSalary DECIMAL(12,2) = 0, @Conveyance DECIMAL(12,2) = 0,
    @BloodGroup NVARCHAR(10) = NULL, @Mobile NVARCHAR(20) = NULL,
    @Email NVARCHAR(150), @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM Employees WHERE Email = @Email AND ID <> @EmployeeID)
        THROW 51003, 'Email already registered', 1;

    UPDATE Employees SET
        FullName = @FullName, CNIC = @CNIC, DepartmentID = @DepartmentID,
        Designation = @Designation, EmployeeType = @EmployeeType, JoiningDate = @JoiningDate,
        BasicSalary = @BasicSalary, Conveyance = @Conveyance, BloodGroup = @BloodGroup,
        Mobile = @Mobile, Email = @Email, IsActive = @IsActive
    WHERE CompanyID = @CompanyID AND ID = @EmployeeID;

    EXEC usp_Employees_GetByID @CompanyID, @EmployeeID;
END
GO

CREATE OR ALTER PROCEDURE usp_Employees_Delete   -- soft delete
    @CompanyID BIGINT, @EmployeeID BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Employees SET IsActive = 0 WHERE CompanyID = @CompanyID AND ID = @EmployeeID;
END
GO

-- =====================================================================
-- DEPARTMENTS
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Departments_List
    @CompanyID BIGINT, @BranchID BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Name, ManagerID
    FROM Departments
    WHERE CompanyID = @CompanyID AND BranchID = @BranchID AND IsActive = 1
    ORDER BY ID;
END
GO

CREATE OR ALTER PROCEDURE usp_Departments_Create
    @CompanyID BIGINT, @BranchID BIGINT, @Name NVARCHAR(150), @ManagerID BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Departments (CompanyID, BranchID, Name, ManagerID)
    VALUES (@CompanyID, @BranchID, @Name, @ManagerID);
    SELECT ID, Name, ManagerID FROM Departments WHERE ID = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE usp_Departments_Update
    @CompanyID BIGINT, @DepartmentID BIGINT, @Name NVARCHAR(150), @ManagerID BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Departments SET Name = @Name, ManagerID = @ManagerID
    WHERE CompanyID = @CompanyID AND ID = @DepartmentID;
    SELECT ID, Name, ManagerID FROM Departments WHERE ID = @DepartmentID;
END
GO

-- =====================================================================
-- ATTENDANCE
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Attendance_ByDate
    @CompanyID BIGINT, @BranchID BIGINT, @Date DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, EmployeeID, Date, Status, TimeIn, TimeOut
    FROM Attendance
    WHERE CompanyID = @CompanyID AND BranchID = @BranchID AND Date = @Date;
END
GO

CREATE OR ALTER PROCEDURE usp_Attendance_Mark    -- upsert on (EmployeeID, Date)
    @CompanyID BIGINT, @BranchID BIGINT, @EmployeeID BIGINT, @Date DATE,
    @Status NVARCHAR(20), @TimeIn TIME = NULL, @TimeOut TIME = NULL, @MarkedByID BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Status = N'Absent' BEGIN SET @TimeIn = NULL; SET @TimeOut = NULL; END

    IF EXISTS (SELECT 1 FROM Attendance WHERE EmployeeID = @EmployeeID AND Date = @Date)
        UPDATE Attendance
        SET Status = @Status, TimeIn = @TimeIn, TimeOut = @TimeOut, MarkedByID = @MarkedByID
        WHERE EmployeeID = @EmployeeID AND Date = @Date;
    ELSE
        INSERT INTO Attendance (CompanyID, BranchID, EmployeeID, Date, Status, TimeIn, TimeOut, MarkedByID)
        VALUES (@CompanyID, @BranchID, @EmployeeID, @Date, @Status, @TimeIn, @TimeOut, @MarkedByID);

    SELECT ID, EmployeeID, Date, Status, TimeIn, TimeOut
    FROM Attendance WHERE EmployeeID = @EmployeeID AND Date = @Date;
END
GO

CREATE OR ALTER PROCEDURE usp_Attendance_History
    @CompanyID BIGINT, @EmployeeID BIGINT, @From DATE = NULL, @To DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, EmployeeID, Date, Status, TimeIn, TimeOut
    FROM Attendance
    WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID
      AND (@From IS NULL OR Date >= @From)
      AND (@To   IS NULL OR Date <= @To)
    ORDER BY Date DESC;
END
GO

-- =====================================================================
-- LEAVES
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_LeaveTypes_List
    @CompanyID BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Name, TotalDays FROM LeaveTypes
    WHERE CompanyID = @CompanyID AND IsActive = 1 ORDER BY ID;
END
GO

CREATE OR ALTER PROCEDURE usp_Leaves_List
    @CompanyID BIGINT, @BranchID BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT la.ID, la.EmployeeID, e.FullName AS EmployeeName, la.LeaveTypeID,
           lt.Name AS TypeName, la.FromDate, la.ToDate, la.TotalDays,
           la.Reason, la.Status, la.CreatedAt
    FROM LeaveApplications la
    JOIN Employees e  ON e.ID  = la.EmployeeID
    JOIN LeaveTypes lt ON lt.ID = la.LeaveTypeID
    WHERE la.CompanyID = @CompanyID AND la.BranchID = @BranchID
    ORDER BY la.ID DESC;
END
GO

CREATE OR ALTER PROCEDURE usp_Leaves_Me
    @CompanyID BIGINT, @EmployeeID BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT la.ID, la.EmployeeID, e.FullName AS EmployeeName, la.LeaveTypeID,
           lt.Name AS TypeName, la.FromDate, la.ToDate, la.TotalDays,
           la.Reason, la.Status, la.CreatedAt
    FROM LeaveApplications la
    JOIN Employees e  ON e.ID  = la.EmployeeID
    JOIN LeaveTypes lt ON lt.ID = la.LeaveTypeID
    WHERE la.CompanyID = @CompanyID AND la.EmployeeID = @EmployeeID
    ORDER BY la.ID DESC;
END
GO

CREATE OR ALTER PROCEDURE usp_Leaves_Apply
    @CompanyID BIGINT, @BranchID BIGINT, @EmployeeID BIGINT, @LeaveTypeID BIGINT,
    @FromDate DATE, @ToDate DATE, @TotalDays INT, @Reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO LeaveApplications (CompanyID, BranchID, EmployeeID, LeaveTypeID,
                                   FromDate, ToDate, TotalDays, Reason)
    VALUES (@CompanyID, @BranchID, @EmployeeID, @LeaveTypeID,
            @FromDate, @ToDate, @TotalDays, @Reason);

    DECLARE @ID BIGINT = SCOPE_IDENTITY();
    SELECT la.ID, la.EmployeeID, e.FullName AS EmployeeName, la.LeaveTypeID,
           lt.Name AS TypeName, la.FromDate, la.ToDate, la.TotalDays,
           la.Reason, la.Status, la.CreatedAt
    FROM LeaveApplications la
    JOIN Employees e  ON e.ID  = la.EmployeeID
    JOIN LeaveTypes lt ON lt.ID = la.LeaveTypeID
    WHERE la.ID = @ID;
END
GO

CREATE OR ALTER PROCEDURE usp_Leaves_SetStatus
    @CompanyID BIGINT, @LeaveID BIGINT, @Status NVARCHAR(20),
    @ApprovedByID BIGINT = NULL, @RejectionReason NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @EmployeeID BIGINT, @LeaveTypeID BIGINT, @Days INT, @BranchID BIGINT, @Year INT;

    SELECT @EmployeeID = EmployeeID, @LeaveTypeID = LeaveTypeID, @Days = TotalDays,
           @BranchID = BranchID, @Year = YEAR(FromDate)
    FROM LeaveApplications
    WHERE CompanyID = @CompanyID AND ID = @LeaveID;

    IF @EmployeeID IS NULL THROW 51004, 'Leave application not found', 1;

    UPDATE LeaveApplications
    SET Status = @Status, ApprovedByID = @ApprovedByID,
        ApprovedDate = CASE WHEN @Status = N'Approved' THEN CAST(GETDATE() AS DATE) ELSE NULL END,
        RejectionReason = @RejectionReason
    WHERE CompanyID = @CompanyID AND ID = @LeaveID;

    IF @Status = N'Approved'
    BEGIN
        -- ensure balance row exists for this year
        IF NOT EXISTS (SELECT 1 FROM LeaveBalances
                       WHERE EmployeeID = @EmployeeID AND LeaveTypeID = @LeaveTypeID AND Year = @Year)
            INSERT INTO LeaveBalances (CompanyID, BranchID, EmployeeID, LeaveTypeID, Year,
                                       TotalDays, UsedDays, RemainingDays)
            SELECT @CompanyID, @BranchID, @EmployeeID, @LeaveTypeID, @Year,
                   lt.TotalDays, 0, lt.TotalDays
            FROM LeaveTypes lt WHERE lt.ID = @LeaveTypeID;

        UPDATE LeaveBalances
        SET UsedDays = UsedDays + @Days,
            RemainingDays = TotalDays - (UsedDays + @Days)
        WHERE EmployeeID = @EmployeeID AND LeaveTypeID = @LeaveTypeID AND Year = @Year;
    END

    COMMIT TRANSACTION;

    SELECT la.ID, la.EmployeeID, e.FullName AS EmployeeName, la.LeaveTypeID,
           lt.Name AS TypeName, la.FromDate, la.ToDate, la.TotalDays,
           la.Reason, la.Status, la.CreatedAt
    FROM LeaveApplications la
    JOIN Employees e  ON e.ID  = la.EmployeeID
    JOIN LeaveTypes lt ON lt.ID = la.LeaveTypeID
    WHERE la.ID = @LeaveID;
END
GO

CREATE OR ALTER PROCEDURE usp_LeaveBalances_Me
    @CompanyID BIGINT, @BranchID BIGINT, @EmployeeID BIGINT, @Year INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ensure a balance row per active leave type for this year
    INSERT INTO LeaveBalances (CompanyID, BranchID, EmployeeID, LeaveTypeID, Year,
                               TotalDays, UsedDays, RemainingDays)
    SELECT @CompanyID, @BranchID, @EmployeeID, lt.ID, @Year, lt.TotalDays, 0, lt.TotalDays
    FROM LeaveTypes lt
    WHERE lt.CompanyID = @CompanyID AND lt.IsActive = 1
      AND NOT EXISTS (SELECT 1 FROM LeaveBalances b
                      WHERE b.EmployeeID = @EmployeeID AND b.LeaveTypeID = lt.ID AND b.Year = @Year);

    SELECT lt.Name AS Type, b.TotalDays, b.UsedDays, b.RemainingDays
    FROM LeaveBalances b
    JOIN LeaveTypes lt ON lt.ID = b.LeaveTypeID
    WHERE b.EmployeeID = @EmployeeID AND b.Year = @Year
    ORDER BY lt.ID;
END
GO

-- =====================================================================
-- LOANS
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Loans_List
    @CompanyID BIGINT, @BranchID BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT l.ID, l.EmployeeID, e.FullName AS EmployeeName, l.LoanType,
           l.TotalAmount, l.MonthlyDeduction, l.RemainingAmount,
           l.Status, l.Reason, l.AppliedDate
    FROM Loans l
    JOIN Employees e ON e.ID = l.EmployeeID
    WHERE l.CompanyID = @CompanyID AND l.BranchID = @BranchID
    ORDER BY l.ID DESC;
END
GO

CREATE OR ALTER PROCEDURE usp_Loans_Me
    @CompanyID BIGINT, @EmployeeID BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT l.ID, l.EmployeeID, e.FullName AS EmployeeName, l.LoanType,
           l.TotalAmount, l.MonthlyDeduction, l.RemainingAmount,
           l.Status, l.Reason, l.AppliedDate
    FROM Loans l
    JOIN Employees e ON e.ID = l.EmployeeID
    WHERE l.CompanyID = @CompanyID AND l.EmployeeID = @EmployeeID
    ORDER BY l.ID DESC;
END
GO

CREATE OR ALTER PROCEDURE usp_Loans_Apply
    @CompanyID BIGINT, @BranchID BIGINT, @EmployeeID BIGINT,
    @LoanType NVARCHAR(50), @Amount DECIMAL(12,2), @Instalment DECIMAL(12,2),
    @Reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Loans (CompanyID, BranchID, EmployeeID, LoanType,
                       TotalAmount, RemainingAmount, MonthlyDeduction, Reason)
    VALUES (@CompanyID, @BranchID, @EmployeeID, @LoanType,
            @Amount, @Amount, @Instalment, @Reason);

    DECLARE @ID BIGINT = SCOPE_IDENTITY();
    SELECT l.ID, l.EmployeeID, e.FullName AS EmployeeName, l.LoanType,
           l.TotalAmount, l.MonthlyDeduction, l.RemainingAmount,
           l.Status, l.Reason, l.AppliedDate
    FROM Loans l JOIN Employees e ON e.ID = l.EmployeeID
    WHERE l.ID = @ID;
END
GO

CREATE OR ALTER PROCEDURE usp_Loans_SetStatus
    @CompanyID BIGINT, @LoanID BIGINT, @Status NVARCHAR(20),
    @ApprovedByID BIGINT = NULL, @RejectionReason NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Loans
    SET Status = @Status, ApprovedByID = @ApprovedByID,
        ApprovedDate = CASE WHEN @Status = N'Approved' THEN CAST(GETDATE() AS DATE) ELSE NULL END,
        RejectionReason = @RejectionReason
    WHERE CompanyID = @CompanyID AND ID = @LoanID;

    SELECT l.ID, l.EmployeeID, e.FullName AS EmployeeName, l.LoanType,
           l.TotalAmount, l.MonthlyDeduction, l.RemainingAmount,
           l.Status, l.Reason, l.AppliedDate
    FROM Loans l JOIN Employees e ON e.ID = l.EmployeeID
    WHERE l.ID = @LoanID;
END
GO

-- =====================================================================
-- PAYROLL OVERRIDES (monthly, pre-run)
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_PayrollOverrides_Get
    @CompanyID BIGINT, @BranchID BIGINT, @Month INT, @Year INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT EmployeeID, Basic, HRA, Medical, Conveyance, Tax, EOBI, Loan, LateDeduction
    FROM EmployeePayrollOverrides
    WHERE CompanyID = @CompanyID AND BranchID = @BranchID
      AND Month = @Month AND Year = @Year;
END
GO

CREATE OR ALTER PROCEDURE usp_PayrollOverrides_Upsert
    @CompanyID BIGINT, @BranchID BIGINT, @EmployeeID BIGINT, @Month INT, @Year INT,
    @Basic DECIMAL(12,2) = NULL, @HRA DECIMAL(12,2) = NULL, @Medical DECIMAL(12,2) = NULL,
    @Conveyance DECIMAL(12,2) = NULL, @Tax DECIMAL(12,2) = NULL, @EOBI DECIMAL(12,2) = NULL,
    @Loan DECIMAL(12,2) = NULL, @LateDeduction DECIMAL(12,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM EmployeePayrollOverrides
               WHERE EmployeeID = @EmployeeID AND Month = @Month AND Year = @Year)
        UPDATE EmployeePayrollOverrides
        SET Basic = @Basic, HRA = @HRA, Medical = @Medical, Conveyance = @Conveyance,
            Tax = @Tax, EOBI = @EOBI, Loan = @Loan, LateDeduction = @LateDeduction,
            UpdatedAt = GETDATE()
        WHERE EmployeeID = @EmployeeID AND Month = @Month AND Year = @Year;
    ELSE
        INSERT INTO EmployeePayrollOverrides
            (CompanyID, BranchID, EmployeeID, Month, Year,
             Basic, HRA, Medical, Conveyance, Tax, EOBI, Loan, LateDeduction)
        VALUES (@CompanyID, @BranchID, @EmployeeID, @Month, @Year,
                @Basic, @HRA, @Medical, @Conveyance, @Tax, @EOBI, @Loan, @LateDeduction);
END
GO

CREATE OR ALTER PROCEDURE usp_PayrollOverrides_Reset   -- "Reset to auto"
    @CompanyID BIGINT, @EmployeeID BIGINT, @Month INT, @Year INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM EmployeePayrollOverrides
    WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID
      AND Month = @Month AND Year = @Year;
END
GO

-- =====================================================================
-- PAYROLL GENERATE — the ONLY creator of payslips.
-- Creates: PayrollRun + PayrollDetails + Payslips + salary expense
-- Transactions; decrements approved loans. Rows arrive as JSON.
-- Throws 51002 if already generated for the month.
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Payroll_Generate
    @CompanyID BIGINT, @BranchID BIGINT, @Month INT, @Year INT,
    @GeneratedByID BIGINT = NULL, @RowsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM PayrollRuns
               WHERE BranchID = @BranchID AND Month = @Month AND Year = @Year)
        THROW 51002, 'Payroll already generated for this month', 1;

    BEGIN TRANSACTION;

    INSERT INTO PayrollRuns (CompanyID, BranchID, Month, Year, GeneratedByID)
    VALUES (@CompanyID, @BranchID, @Month, @Year, @GeneratedByID);

    DECLARE @RunID BIGINT = SCOPE_IDENTITY();

    SELECT *
    INTO #Rows
    FROM OPENJSON(@RowsJson)
    WITH (
        EmployeeID      BIGINT          '$.employeeId',
        Basic           DECIMAL(12,2)   '$.basic',
        HRA             DECIMAL(12,2)   '$.hra',
        Medical         DECIMAL(12,2)   '$.medical',
        Conveyance      DECIMAL(12,2)   '$.conveyance',
        Gross           DECIMAL(12,2)   '$.gross',
        Tax             DECIMAL(12,2)   '$.tax',
        EOBI            DECIMAL(12,2)   '$.eobi',
        Loan            DECIMAL(12,2)   '$.loan',
        LateDeduction   DECIMAL(12,2)   '$.lateDeduction',
        TotalDeductions DECIMAL(12,2)   '$.totalDeductions',
        Net             DECIMAL(12,2)   '$.net',
        WorkingDays     INT             '$.workingDays',
        PresentDays     INT             '$.presentDays',
        AbsentDays      INT             '$.absentDays',
        LateDays        INT             '$.lateDays',
        IsOverridden    BIT             '$.isOverridden'
    );

    INSERT INTO PayrollDetails
        (CompanyID, BranchID, PayrollRunID, EmployeeID,
         BasicSalary, HRA, Medical, Conveyance, GrossSalary,
         LoanDeduction, LateDeduction, IncomeTax, EOBI, NetSalary,
         WorkingDays, PresentDays, AbsentDays, LateDays, IsOverridden)
    SELECT @CompanyID, @BranchID, @RunID, EmployeeID,
           ISNULL(Basic,0), ISNULL(HRA,0), ISNULL(Medical,0), ISNULL(Conveyance,0), ISNULL(Gross,0),
           ISNULL(Loan,0), ISNULL(LateDeduction,0), ISNULL(Tax,0), ISNULL(EOBI,0), ISNULL(Net,0),
           ISNULL(WorkingDays,0), ISNULL(PresentDays,0), ISNULL(AbsentDays,0), ISNULL(LateDays,0),
           ISNULL(IsOverridden,0)
    FROM #Rows;

    INSERT INTO Payslips (CompanyID, BranchID, PayrollDetailID, EmployeeID, Month, Year, NetSalary)
    SELECT @CompanyID, @BranchID, pd.ID, pd.EmployeeID, @Month, @Year, pd.NetSalary
    FROM PayrollDetails pd
    WHERE pd.PayrollRunID = @RunID;

    -- Salary expense transaction per generated payslip (business rule #4 / #8)
    INSERT INTO Transactions (CompanyID, BranchID, Type, Category, Description, Amount,
                              TransactionDate, PayslipID, CreatedByID)
    SELECT @CompanyID, @BranchID, N'Expense', N'Salary',
           CONCAT(N'Salary — ', e.FullName, N' (', @Year, N'-', FORMAT(@Month, '00'), N')'),
           ps.NetSalary, CAST(GETDATE() AS DATE), ps.ID, @GeneratedByID
    FROM Payslips ps
    JOIN Employees e ON e.ID = ps.EmployeeID
    JOIN PayrollDetails pd ON pd.ID = ps.PayrollDetailID
    WHERE pd.PayrollRunID = @RunID;

    -- Decrement approved loans by deducted amount; mark Cleared at 0
    UPDATE l
    SET l.RemainingAmount = CASE WHEN l.RemainingAmount - r.Loan < 0
                                 THEN 0 ELSE l.RemainingAmount - r.Loan END,
        l.Status = CASE WHEN l.RemainingAmount - r.Loan <= 0
                        THEN N'Cleared' ELSE l.Status END
    FROM Loans l
    JOIN #Rows r ON r.EmployeeID = l.EmployeeID
    WHERE l.CompanyID = @CompanyID AND l.Status = N'Approved' AND ISNULL(r.Loan, 0) > 0;

    COMMIT TRANSACTION;

    SELECT @RunID AS PayrollRunID, (SELECT COUNT(*) FROM #Rows) AS SlipCount;
END
GO

-- =====================================================================
-- PAYSLIPS
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Payslips_List
    @CompanyID BIGINT, @BranchID BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ps.ID, ps.EmployeeID, e.FullName AS EmployeeName, ps.Month, ps.Year,
           pd.BasicSalary, pd.HRA, pd.Medical, pd.Conveyance, pd.GrossSalary,
           pd.IncomeTax, pd.EOBI, pd.LoanDeduction, pd.LateDeduction, pd.NetSalary,
           ps.GeneratedAt
    FROM Payslips ps
    JOIN PayrollDetails pd ON pd.ID = ps.PayrollDetailID
    JOIN Employees e       ON e.ID  = ps.EmployeeID
    WHERE ps.CompanyID = @CompanyID AND ps.BranchID = @BranchID
    ORDER BY ps.Year DESC, ps.Month DESC, e.FullName;
END
GO

CREATE OR ALTER PROCEDURE usp_Payslips_ByEmployee
    @CompanyID BIGINT, @EmployeeID BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ps.ID, ps.EmployeeID, e.FullName AS EmployeeName, ps.Month, ps.Year,
           pd.BasicSalary, pd.HRA, pd.Medical, pd.Conveyance, pd.GrossSalary,
           pd.IncomeTax, pd.EOBI, pd.LoanDeduction, pd.LateDeduction, pd.NetSalary,
           ps.GeneratedAt
    FROM Payslips ps
    JOIN PayrollDetails pd ON pd.ID = ps.PayrollDetailID
    JOIN Employees e       ON e.ID  = ps.EmployeeID
    WHERE ps.CompanyID = @CompanyID AND ps.EmployeeID = @EmployeeID
    ORDER BY ps.Year DESC, ps.Month DESC;
END
GO

CREATE OR ALTER PROCEDURE usp_Payslips_Overview
    @CompanyID BIGINT, @BranchID BIGINT, @Month INT, @Year INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.ID AS EmployeeID, e.FullName,
           CAST(CASE WHEN ps.ID IS NULL THEN 0 ELSE 1 END AS BIT) AS Generated,
           ps.NetSalary
    FROM Employees e
    LEFT JOIN Payslips ps ON ps.EmployeeID = e.ID AND ps.Month = @Month AND ps.Year = @Year
    WHERE e.CompanyID = @CompanyID AND e.BranchID = @BranchID AND e.IsActive = 1
    ORDER BY e.FullName;
END
GO

-- =====================================================================
-- ACCOUNTS — TRANSACTIONS (expenses / revenues)
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Transactions_List
    @CompanyID BIGINT, @BranchID BIGINT, @Type NVARCHAR(20),
    @Month INT = NULL, @Year INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, TransactionDate, Category, Description, Amount, PayslipID
    FROM Transactions
    WHERE CompanyID = @CompanyID AND BranchID = @BranchID AND Type = @Type
      AND (@Month IS NULL OR (MONTH(TransactionDate) = @Month AND YEAR(TransactionDate) = @Year))
    ORDER BY TransactionDate DESC, ID DESC;
END
GO

CREATE OR ALTER PROCEDURE usp_Transactions_Create
    @CompanyID BIGINT, @BranchID BIGINT, @Type NVARCHAR(20),
    @Category NVARCHAR(100), @Description NVARCHAR(500) = NULL,
    @Amount DECIMAL(12,2), @TransactionDate DATE, @CreatedByID BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Transactions (CompanyID, BranchID, Type, Category, Description,
                              Amount, TransactionDate, CreatedByID)
    VALUES (@CompanyID, @BranchID, @Type, @Category, @Description,
            @Amount, @TransactionDate, @CreatedByID);

    SELECT ID, TransactionDate, Category, Description, Amount, PayslipID
    FROM Transactions WHERE ID = SCOPE_IDENTITY();
END
GO

-- =====================================================================
-- ACCOUNTS — MONTHLY REPORT (3 result sets: expense groups, revenue
-- groups, salary slip count). Salaries grouped separately.
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Accounts_Report
    @CompanyID BIGINT, @BranchID BIGINT, @Month INT, @Year INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1) Expense groups (salaries from generated payslips first)
    SELECT
        CASE WHEN PayslipID IS NOT NULL THEN N'Salaries (generated payslips)'
             ELSE ISNULL(Category, N'Other') END AS Head,
        SUM(Amount) AS Amount
    FROM Transactions
    WHERE CompanyID = @CompanyID AND BranchID = @BranchID AND Type = N'Expense'
      AND MONTH(TransactionDate) = @Month AND YEAR(TransactionDate) = @Year
    GROUP BY CASE WHEN PayslipID IS NOT NULL THEN N'Salaries (generated payslips)'
                  ELSE ISNULL(Category, N'Other') END
    ORDER BY CASE WHEN CASE WHEN PayslipID IS NOT NULL THEN N'Salaries (generated payslips)'
                            ELSE ISNULL(Category, N'Other') END
                       = N'Salaries (generated payslips)' THEN 0 ELSE 1 END,
             Amount DESC;

    -- 2) Revenue groups by source
    SELECT ISNULL(Category, N'Other') AS Head, SUM(Amount) AS Amount
    FROM Transactions
    WHERE CompanyID = @CompanyID AND BranchID = @BranchID AND Type = N'Revenue'
      AND MONTH(TransactionDate) = @Month AND YEAR(TransactionDate) = @Year
    GROUP BY ISNULL(Category, N'Other')
    ORDER BY Amount DESC;

    -- 3) Salary slip count for the month
    SELECT COUNT(*) AS SalaryCount
    FROM Payslips
    WHERE CompanyID = @CompanyID AND BranchID = @BranchID
      AND Month = @Month AND Year = @Year;
END
GO

-- =====================================================================
-- APPRAISALS
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Appraisals_List
    @CompanyID BIGINT, @BranchID BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.ID, a.EmployeeID, e.FullName AS EmployeeName, a.Cycle, a.Goals,
           a.Rating, a.ManagerComment, a.Status
    FROM Appraisals a
    JOIN Employees e ON e.ID = a.EmployeeID
    WHERE a.CompanyID = @CompanyID AND a.BranchID = @BranchID
    ORDER BY a.ID DESC;
END
GO

CREATE OR ALTER PROCEDURE usp_Appraisals_Create
    @CompanyID BIGINT, @BranchID BIGINT, @EmployeeID BIGINT, @Cycle NVARCHAR(50),
    @Goals NVARCHAR(1000) = NULL, @Rating INT = NULL,
    @ManagerComment NVARCHAR(1000) = NULL, @Status NVARCHAR(20) = N'Draft'
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Appraisals (CompanyID, BranchID, EmployeeID, Cycle, Goals,
                            Rating, ManagerComment, Status)
    VALUES (@CompanyID, @BranchID, @EmployeeID, @Cycle, @Goals,
            @Rating, @ManagerComment, @Status);

    SELECT a.ID, a.EmployeeID, e.FullName AS EmployeeName, a.Cycle, a.Goals,
           a.Rating, a.ManagerComment, a.Status
    FROM Appraisals a JOIN Employees e ON e.ID = a.EmployeeID
    WHERE a.ID = SCOPE_IDENTITY();
END
GO

PRINT 'All module stored procedures created ✔';
GO
