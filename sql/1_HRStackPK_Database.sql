-- =====================================================================
-- HRStackPK — Complete Database Creation Script
-- Database: HRStackPK
-- Version: 1.0
-- =====================================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'HRStackPK')
BEGIN
    CREATE DATABASE HRStackPK;
END
GO

USE HRStackPK;
GO

-- =====================================================================
-- 1. COMPANIES (Master Tenant — created on signup)
-- =====================================================================
CREATE TABLE Companies (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyName     NVARCHAR(200)   NOT NULL,
    Email           NVARCHAR(150),
    Mobile          NVARCHAR(20)    NOT NULL UNIQUE,  -- Head login identifier
    Password        NVARCHAR(255)   NOT NULL,
    LogoURL         NVARCHAR(500),
    Address         NVARCHAR(500),
    City            NVARCHAR(100),
    Country         NVARCHAR(100)   DEFAULT 'Pakistan',
    IsActive        BIT             DEFAULT 1,
    CreatedAt       DATETIME        DEFAULT GETDATE()
);
GO

-- =====================================================================
-- 2. BRANCHES (Each company can have multiple branches)
-- =====================================================================
CREATE TABLE Branches (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID       BIGINT          NOT NULL,
    BranchName      NVARCHAR(200)   NOT NULL,
    Address         NVARCHAR(500),
    City            NVARCHAR(100),
    Phone           NVARCHAR(20),
    IsActive        BIT             DEFAULT 1,
    CreatedAt       DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_Branches_Companies FOREIGN KEY (CompanyID) REFERENCES Companies(ID)
);
GO

-- =====================================================================
-- 3. DEPARTMENTS
-- =====================================================================
CREATE TABLE Departments (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID       BIGINT          NOT NULL,
    BranchID        BIGINT          NOT NULL,
    Name            NVARCHAR(150)   NOT NULL,
    IsActive        BIT             DEFAULT 1,
    CreatedAt       DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_Departments_Companies  FOREIGN KEY (CompanyID) REFERENCES Companies(ID),
    CONSTRAINT FK_Departments_Branches   FOREIGN KEY (BranchID)  REFERENCES Branches(ID)
);
GO

-- =====================================================================
-- 4. ROLES (System default + company custom roles)
-- =====================================================================
CREATE TABLE Roles (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID       BIGINT,                           -- NULL = system default role
    RoleName        NVARCHAR(100)   NOT NULL,
    IsSystem        BIT             DEFAULT 0,        -- 1 = cannot be deleted
    IsActive        BIT             DEFAULT 1,
    CreatedAt       DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_Roles_Companies FOREIGN KEY (CompanyID) REFERENCES Companies(ID)
);
GO

-- Seed default system roles
INSERT INTO Roles (CompanyID, RoleName, IsSystem) VALUES
(NULL, 'Head',          1),
(NULL, 'HR Manager',    1),
(NULL, 'Accountant',    1),
(NULL, 'Staff',         1);
GO

-- =====================================================================
-- 5. EMPLOYEES
-- =====================================================================
CREATE TABLE Employees (
    ID                  BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID           BIGINT          NOT NULL,
    BranchID            BIGINT          NOT NULL,
    DepartmentID        BIGINT,
    FullName            NVARCHAR(200)   NOT NULL,
    Email               NVARCHAR(150)   UNIQUE,       -- Staff login identifier
    Mobile              NVARCHAR(20),
    CNIC                NVARCHAR(20),
    Designation         NVARCHAR(150),
    EmployeeType        NVARCHAR(50),                 -- Permanent / Contract / Probation
    JoiningDate         DATE,
    IsActive            BIT             DEFAULT 1,
    Password            NVARCHAR(255)   NOT NULL,
    ProfileImageURL     NVARCHAR(500),
    CreatedAt           DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_Employees_Companies   FOREIGN KEY (CompanyID)     REFERENCES Companies(ID),
    CONSTRAINT FK_Employees_Branches    FOREIGN KEY (BranchID)      REFERENCES Branches(ID),
    CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentID)  REFERENCES Departments(ID)
);
GO

-- =====================================================================
-- 6. EMPLOYEE ROLES (Employee ko role assign karna)
-- =====================================================================
CREATE TABLE EmployeeRoles (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID       BIGINT          NOT NULL,
    BranchID        BIGINT          NOT NULL,
    EmployeeID      BIGINT          NOT NULL,
    RoleID          BIGINT          NOT NULL,
    AssignedByID    BIGINT,                           -- Head/HR ne assign kiya
    AssignedAt      DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_EmployeeRoles_Companies   FOREIGN KEY (CompanyID)     REFERENCES Companies(ID),
    CONSTRAINT FK_EmployeeRoles_Branches    FOREIGN KEY (BranchID)      REFERENCES Branches(ID),
    CONSTRAINT FK_EmployeeRoles_Employees   FOREIGN KEY (EmployeeID)    REFERENCES Employees(ID),
    CONSTRAINT FK_EmployeeRoles_Roles       FOREIGN KEY (RoleID)        REFERENCES Roles(ID),
    CONSTRAINT FK_EmployeeRoles_AssignedBy  FOREIGN KEY (AssignedByID)  REFERENCES Employees(ID),
    CONSTRAINT UQ_EmployeeRoles             UNIQUE (EmployeeID, RoleID)
);
GO

-- =====================================================================
-- 7. MODULES (All features/modules in the system)
-- =====================================================================
CREATE TABLE Modules (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    ModuleName      NVARCHAR(100)   NOT NULL,
    ModuleKey       NVARCHAR(100)   NOT NULL UNIQUE,  -- used in API & frontend
    IsActive        BIT             DEFAULT 1
);
GO

-- Seed modules
INSERT INTO Modules (ModuleName, ModuleKey) VALUES
('Dashboard',           'dashboard'),
('Employees',           'employees'),
('Departments',         'departments'),
('Attendance',          'attendance'),
('Payroll',             'payroll'),
('Payslips',            'payslips'),
('Leaves',              'leaves'),
('Loans',               'loans'),
('Accounts',            'accounts'),
('Reports',             'reports'),
('Company Settings',    'settings'),
('Roles & Permissions', 'roles');
GO

-- =====================================================================
-- 8. PERMISSIONS (Actions available per module)
-- =====================================================================
CREATE TABLE Permissions (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    ModuleID        BIGINT          NOT NULL,
    PermissionName  NVARCHAR(100)   NOT NULL,          -- View, Add, Edit, Delete, Approve
    PermissionKey   NVARCHAR(100)   NOT NULL,          -- view, add, edit, delete, approve

    CONSTRAINT FK_Permissions_Modules FOREIGN KEY (ModuleID) REFERENCES Modules(ID)
);
GO

-- Seed permissions
-- Dashboard (ModuleID=1)
INSERT INTO Permissions (ModuleID, PermissionName, PermissionKey) VALUES
(1, 'View',     'view'),

-- Employees (ModuleID=2)
(2, 'View',     'view'),
(2, 'Add',      'add'),
(2, 'Edit',     'edit'),
(2, 'Delete',   'delete'),

-- Departments (ModuleID=3)
(3, 'View',     'view'),
(3, 'Add',      'add'),
(3, 'Edit',     'edit'),
(3, 'Delete',   'delete'),

-- Attendance (ModuleID=4)
(4, 'View',     'view'),
(4, 'Mark',     'mark'),
(4, 'Edit',     'edit'),

-- Payroll (ModuleID=5)
(5, 'View',     'view'),
(5, 'Run',      'run'),
(5, 'Edit',     'edit'),

-- Payslips (ModuleID=6)
(6, 'View',     'view'),

-- Leaves (ModuleID=7)
(7, 'View',     'view'),
(7, 'Apply',    'apply'),
(7, 'Approve',  'approve'),

-- Loans (ModuleID=8)
(8, 'View',     'view'),
(8, 'Apply',    'apply'),
(8, 'Approve',  'approve'),

-- Accounts (ModuleID=9)
(9, 'View',     'view'),
(9, 'Add',      'add'),
(9, 'Edit',     'edit'),
(9, 'Delete',   'delete'),

-- Reports (ModuleID=10)
(10, 'View',    'view'),
(10, 'Export',  'export'),

-- Settings (ModuleID=11)
(11, 'View',    'view'),
(11, 'Edit',    'edit'),

-- Roles (ModuleID=12)
(12, 'View',    'view'),
(12, 'Add',     'add'),
(12, 'Edit',    'edit'),
(12, 'Delete',  'delete'),
(12, 'Assign',  'assign');
GO

-- =====================================================================
-- 9. ROLE PERMISSIONS (Which role has which permission)
-- =====================================================================
CREATE TABLE RolePermissions (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID       BIGINT          NOT NULL,
    RoleID          BIGINT          NOT NULL,
    PermissionID    BIGINT          NOT NULL,
    IsAllowed       BIT             DEFAULT 1,
    CreatedAt       DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_RolePermissions_Companies     FOREIGN KEY (CompanyID)     REFERENCES Companies(ID),
    CONSTRAINT FK_RolePermissions_Roles         FOREIGN KEY (RoleID)        REFERENCES Roles(ID),
    CONSTRAINT FK_RolePermissions_Permissions   FOREIGN KEY (PermissionID)  REFERENCES Permissions(ID),
    CONSTRAINT UQ_RolePermissions               UNIQUE (CompanyID, RoleID, PermissionID)
);
GO

-- =====================================================================
-- 10. EMPLOYEE PERMISSIONS (Individual override per employee)
--     Head kisi specific employee ko extra/restricted permission de sakta hai
-- =====================================================================
CREATE TABLE EmployeePermissions (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID       BIGINT          NOT NULL,
    BranchID        BIGINT          NOT NULL,
    EmployeeID      BIGINT          NOT NULL,
    PermissionID    BIGINT          NOT NULL,
    IsAllowed       BIT             DEFAULT 1,         -- 1=grant, 0=revoke override
    GrantedByID     BIGINT,
    GrantedAt       DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_EmpPermissions_Companies   FOREIGN KEY (CompanyID)     REFERENCES Companies(ID),
    CONSTRAINT FK_EmpPermissions_Branches    FOREIGN KEY (BranchID)      REFERENCES Branches(ID),
    CONSTRAINT FK_EmpPermissions_Employees   FOREIGN KEY (EmployeeID)    REFERENCES Employees(ID),
    CONSTRAINT FK_EmpPermissions_Permissions FOREIGN KEY (PermissionID)  REFERENCES Permissions(ID),
    CONSTRAINT FK_EmpPermissions_GrantedBy   FOREIGN KEY (GrantedByID)   REFERENCES Employees(ID),
    CONSTRAINT UQ_EmployeePermissions        UNIQUE (EmployeeID, PermissionID)
);
GO

-- =====================================================================
-- 11. SALARY STRUCTURES (Per employee salary setup)
-- =====================================================================
CREATE TABLE SalaryStructures (
    ID                  BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID           BIGINT          NOT NULL,
    BranchID            BIGINT          NOT NULL,
    EmployeeID          BIGINT          NOT NULL,
    BasicSalary         DECIMAL(12,2)   DEFAULT 0,
    HRA                 DECIMAL(12,2)   DEFAULT 0,      -- 40% of basic
    Medical             DECIMAL(12,2)   DEFAULT 0,      -- 10% of basic
    Conveyance          DECIMAL(12,2)   DEFAULT 0,
    OtherAllowance      DECIMAL(12,2)   DEFAULT 0,
    GrossSalary         DECIMAL(12,2)   DEFAULT 0,
    EOBI_Employee       DECIMAL(12,2)   DEFAULT 370,
    EOBI_Employer       DECIMAL(12,2)   DEFAULT 1850,
    IncomeTax           DECIMAL(12,2)   DEFAULT 0,
    EffectiveFrom       DATE,
    CreatedAt           DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_SalaryStructures_Companies  FOREIGN KEY (CompanyID)  REFERENCES Companies(ID),
    CONSTRAINT FK_SalaryStructures_Branches   FOREIGN KEY (BranchID)   REFERENCES Branches(ID),
    CONSTRAINT FK_SalaryStructures_Employees  FOREIGN KEY (EmployeeID) REFERENCES Employees(ID)
);
GO

-- =====================================================================
-- 12. ATTENDANCE
-- =====================================================================
CREATE TABLE Attendance (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID       BIGINT          NOT NULL,
    BranchID        BIGINT          NOT NULL,
    EmployeeID      BIGINT          NOT NULL,
    Date            DATE            NOT NULL,
    Status          NVARCHAR(20)    NOT NULL,           -- Present / Absent / Late
    TimeIn          TIME,
    TimeOut         TIME,
    MarkedByID      BIGINT,                             -- who marked it (Head/HR or self check-in)
    CreatedAt       DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_Attendance_Companies  FOREIGN KEY (CompanyID)  REFERENCES Companies(ID),
    CONSTRAINT FK_Attendance_Branches   FOREIGN KEY (BranchID)   REFERENCES Branches(ID),
    CONSTRAINT FK_Attendance_Employees  FOREIGN KEY (EmployeeID) REFERENCES Employees(ID),
    CONSTRAINT FK_Attendance_MarkedBy   FOREIGN KEY (MarkedByID) REFERENCES Employees(ID),
    CONSTRAINT UQ_Attendance            UNIQUE (EmployeeID, Date)
);
GO

-- =====================================================================
-- 13. LEAVE TYPES
-- =====================================================================
CREATE TABLE LeaveTypes (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID       BIGINT          NOT NULL,
    Name            NVARCHAR(100)   NOT NULL,           -- Annual / Sick / Casual
    TotalDays       INT             DEFAULT 0,
    IsActive        BIT             DEFAULT 1,

    CONSTRAINT FK_LeaveTypes_Companies FOREIGN KEY (CompanyID) REFERENCES Companies(ID)
);
GO

-- Seed default leave types (will be inserted per company on signup)
-- Annual=15, Sick=10, Casual=5 (common Pakistani HR policy)

-- =====================================================================
-- 14. LEAVE APPLICATIONS
-- =====================================================================
CREATE TABLE LeaveApplications (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID       BIGINT          NOT NULL,
    BranchID        BIGINT          NOT NULL,
    EmployeeID      BIGINT          NOT NULL,
    LeaveTypeID     BIGINT          NOT NULL,
    FromDate        DATE            NOT NULL,
    ToDate          DATE            NOT NULL,
    TotalDays       INT             NOT NULL,
    Reason          NVARCHAR(500),
    Status          NVARCHAR(20)    DEFAULT 'Pending',  -- Pending / Approved / Rejected
    ApprovedByID    BIGINT,
    ApprovedDate    DATE,
    RejectionReason NVARCHAR(300),
    CreatedAt       DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_LeaveApps_Companies   FOREIGN KEY (CompanyID)     REFERENCES Companies(ID),
    CONSTRAINT FK_LeaveApps_Branches    FOREIGN KEY (BranchID)      REFERENCES Branches(ID),
    CONSTRAINT FK_LeaveApps_Employees   FOREIGN KEY (EmployeeID)    REFERENCES Employees(ID),
    CONSTRAINT FK_LeaveApps_LeaveTypes  FOREIGN KEY (LeaveTypeID)   REFERENCES LeaveTypes(ID),
    CONSTRAINT FK_LeaveApps_ApprovedBy  FOREIGN KEY (ApprovedByID)  REFERENCES Employees(ID)
);
GO

-- =====================================================================
-- 15. LEAVE BALANCES (Remaining leave per employee per year)
-- =====================================================================
CREATE TABLE LeaveBalances (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID       BIGINT          NOT NULL,
    BranchID        BIGINT          NOT NULL,
    EmployeeID      BIGINT          NOT NULL,
    LeaveTypeID     BIGINT          NOT NULL,
    Year            INT             NOT NULL,
    TotalDays       INT             DEFAULT 0,
    UsedDays        INT             DEFAULT 0,
    RemainingDays   INT             DEFAULT 0,

    CONSTRAINT FK_LeaveBalances_Companies   FOREIGN KEY (CompanyID)     REFERENCES Companies(ID),
    CONSTRAINT FK_LeaveBalances_Branches    FOREIGN KEY (BranchID)      REFERENCES Branches(ID),
    CONSTRAINT FK_LeaveBalances_Employees   FOREIGN KEY (EmployeeID)    REFERENCES Employees(ID),
    CONSTRAINT FK_LeaveBalances_LeaveTypes  FOREIGN KEY (LeaveTypeID)   REFERENCES LeaveTypes(ID),
    CONSTRAINT UQ_LeaveBalances             UNIQUE (EmployeeID, LeaveTypeID, Year)
);
GO

-- =====================================================================
-- 16. LOANS & ADVANCES
-- =====================================================================
CREATE TABLE Loans (
    ID                  BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID           BIGINT          NOT NULL,
    BranchID            BIGINT          NOT NULL,
    EmployeeID          BIGINT          NOT NULL,
    LoanType            NVARCHAR(50)    NOT NULL,       -- Loan / Advance
    TotalAmount         DECIMAL(12,2)   NOT NULL,
    RemainingAmount     DECIMAL(12,2)   NOT NULL,
    MonthlyDeduction    DECIMAL(12,2)   DEFAULT 0,
    Status              NVARCHAR(20)    DEFAULT 'Pending', -- Pending/Approved/Rejected/Cleared
    Reason              NVARCHAR(500),
    AppliedDate         DATE            DEFAULT GETDATE(),
    ApprovedDate        DATE,
    ApprovedByID        BIGINT,
    RejectionReason     NVARCHAR(300),
    CreatedAt           DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_Loans_Companies   FOREIGN KEY (CompanyID)     REFERENCES Companies(ID),
    CONSTRAINT FK_Loans_Branches    FOREIGN KEY (BranchID)      REFERENCES Branches(ID),
    CONSTRAINT FK_Loans_Employees   FOREIGN KEY (EmployeeID)    REFERENCES Employees(ID),
    CONSTRAINT FK_Loans_ApprovedBy  FOREIGN KEY (ApprovedByID)  REFERENCES Employees(ID)
);
GO

-- =====================================================================
-- 17. PAYROLL RUNS (One record per month/year per branch)
-- =====================================================================
CREATE TABLE PayrollRuns (
    ID                  BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID           BIGINT          NOT NULL,
    BranchID            BIGINT          NOT NULL,
    Month               INT             NOT NULL,       -- 1-12
    Year                INT             NOT NULL,
    RunDate             DATETIME        DEFAULT GETDATE(),
    GeneratedByID       BIGINT,
    Status              NVARCHAR(20)    DEFAULT 'Generated', -- Generated / Finalized
    CreatedAt           DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_PayrollRuns_Companies     FOREIGN KEY (CompanyID)         REFERENCES Companies(ID),
    CONSTRAINT FK_PayrollRuns_Branches      FOREIGN KEY (BranchID)          REFERENCES Branches(ID),
    CONSTRAINT FK_PayrollRuns_GeneratedBy   FOREIGN KEY (GeneratedByID)     REFERENCES Employees(ID),
    CONSTRAINT UQ_PayrollRuns               UNIQUE (BranchID, Month, Year)
);
GO

-- =====================================================================
-- 18. PAYROLL DETAILS (Per employee payroll breakdown)
-- =====================================================================
CREATE TABLE PayrollDetails (
    ID                  BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID           BIGINT          NOT NULL,
    BranchID            BIGINT          NOT NULL,
    PayrollRunID        BIGINT          NOT NULL,
    EmployeeID          BIGINT          NOT NULL,
    BasicSalary         DECIMAL(12,2)   DEFAULT 0,
    HRA                 DECIMAL(12,2)   DEFAULT 0,
    Medical             DECIMAL(12,2)   DEFAULT 0,
    Conveyance          DECIMAL(12,2)   DEFAULT 0,
    OtherAllowance      DECIMAL(12,2)   DEFAULT 0,
    GrossSalary         DECIMAL(12,2)   DEFAULT 0,
    LoanDeduction       DECIMAL(12,2)   DEFAULT 0,
    LateDeduction       DECIMAL(12,2)   DEFAULT 0,      -- 3 late = 1 day basic deduction
    IncomeTax           DECIMAL(12,2)   DEFAULT 0,
    EOBI                DECIMAL(12,2)   DEFAULT 370,
    OtherDeduction      DECIMAL(12,2)   DEFAULT 0,
    NetSalary           DECIMAL(12,2)   DEFAULT 0,
    WorkingDays         INT             DEFAULT 0,
    PresentDays         INT             DEFAULT 0,
    AbsentDays          INT             DEFAULT 0,
    LateDays            INT             DEFAULT 0,
    IsOverridden        BIT             DEFAULT 0,      -- manual edit flag
    CreatedAt           DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_PayrollDetails_Companies      FOREIGN KEY (CompanyID)     REFERENCES Companies(ID),
    CONSTRAINT FK_PayrollDetails_Branches       FOREIGN KEY (BranchID)      REFERENCES Branches(ID),
    CONSTRAINT FK_PayrollDetails_PayrollRuns    FOREIGN KEY (PayrollRunID)  REFERENCES PayrollRuns(ID),
    CONSTRAINT FK_PayrollDetails_Employees      FOREIGN KEY (EmployeeID)    REFERENCES Employees(ID)
);
GO

-- =====================================================================
-- 19. PAYROLL OVERRIDES (Reset to auto tracking)
-- =====================================================================
CREATE TABLE PayrollOverrides (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID       BIGINT          NOT NULL,
    PayrollDetailID BIGINT          NOT NULL,
    FieldName       NVARCHAR(100)   NOT NULL,           -- BasicSalary, HRA, etc
    OriginalValue   DECIMAL(12,2),
    OverriddenValue DECIMAL(12,2),
    OverriddenByID  BIGINT,
    OverriddenAt    DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_PayrollOverrides_Companies        FOREIGN KEY (CompanyID)         REFERENCES Companies(ID),
    CONSTRAINT FK_PayrollOverrides_PayrollDetails   FOREIGN KEY (PayrollDetailID)   REFERENCES PayrollDetails(ID),
    CONSTRAINT FK_PayrollOverrides_Employees        FOREIGN KEY (OverriddenByID)    REFERENCES Employees(ID)
);
GO

-- =====================================================================
-- 20. PAYSLIPS (Generated payslip records)
-- =====================================================================
CREATE TABLE Payslips (
    ID                  BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID           BIGINT          NOT NULL,
    BranchID            BIGINT          NOT NULL,
    PayrollDetailID     BIGINT          NOT NULL,
    EmployeeID          BIGINT          NOT NULL,
    Month               INT             NOT NULL,
    Year                INT             NOT NULL,
    NetSalary           DECIMAL(12,2)   NOT NULL,
    GeneratedAt         DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_Payslips_Companies        FOREIGN KEY (CompanyID)         REFERENCES Companies(ID),
    CONSTRAINT FK_Payslips_Branches         FOREIGN KEY (BranchID)          REFERENCES Branches(ID),
    CONSTRAINT FK_Payslips_PayrollDetails   FOREIGN KEY (PayrollDetailID)   REFERENCES PayrollDetails(ID),
    CONSTRAINT FK_Payslips_Employees        FOREIGN KEY (EmployeeID)        REFERENCES Employees(ID)
);
GO

-- =====================================================================
-- 21. TRANSACTIONS (Accounts — expense/revenue)
-- =====================================================================
CREATE TABLE Transactions (
    ID                  BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID           BIGINT          NOT NULL,
    BranchID            BIGINT          NOT NULL,
    Type                NVARCHAR(20)    NOT NULL,       -- Expense / Revenue
    Category            NVARCHAR(100),                  -- Salary / Rent / Manual etc
    Description         NVARCHAR(500),
    Amount              DECIMAL(12,2)   NOT NULL,
    TransactionDate     DATE            NOT NULL,
    PayslipID           BIGINT,                         -- NULL for manual entries
    CreatedByID         BIGINT,
    CreatedAt           DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_Transactions_Companies    FOREIGN KEY (CompanyID)     REFERENCES Companies(ID),
    CONSTRAINT FK_Transactions_Branches     FOREIGN KEY (BranchID)      REFERENCES Branches(ID),
    CONSTRAINT FK_Transactions_Payslips     FOREIGN KEY (PayslipID)     REFERENCES Payslips(ID),
    CONSTRAINT FK_Transactions_CreatedBy    FOREIGN KEY (CreatedByID)   REFERENCES Employees(ID)
);
GO

-- =====================================================================
-- 22. REFRESH TOKENS (JWT refresh token management)
-- =====================================================================
CREATE TABLE RefreshTokens (
    ID              BIGINT PRIMARY KEY IDENTITY(1,1),
    CompanyID       BIGINT          NOT NULL,
    EmployeeID      BIGINT,                             -- NULL if Head login
    IsHeadLogin     BIT             DEFAULT 0,
    Token           NVARCHAR(500)   NOT NULL UNIQUE,
    ExpiresAt       DATETIME        NOT NULL,
    IsRevoked       BIT             DEFAULT 0,
    CreatedAt       DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_RefreshTokens_Companies   FOREIGN KEY (CompanyID)     REFERENCES Companies(ID),
    CONSTRAINT FK_RefreshTokens_Employees   FOREIGN KEY (EmployeeID)    REFERENCES Employees(ID)
);
GO

-- =====================================================================
-- INDEXES (Performance)
-- =====================================================================
CREATE INDEX IX_Branches_CompanyID         ON Branches(CompanyID);
CREATE INDEX IX_Departments_BranchID       ON Departments(BranchID);
CREATE INDEX IX_Employees_BranchID         ON Employees(BranchID);
CREATE INDEX IX_Employees_Email            ON Employees(Email);
CREATE INDEX IX_Attendance_EmployeeDate    ON Attendance(EmployeeID, Date);
CREATE INDEX IX_Attendance_BranchDate      ON Attendance(BranchID, Date);
CREATE INDEX IX_LeaveApps_EmployeeID       ON LeaveApplications(EmployeeID);
CREATE INDEX IX_LeaveApps_Status           ON LeaveApplications(Status);
CREATE INDEX IX_Loans_EmployeeID           ON Loans(EmployeeID);
CREATE INDEX IX_Loans_Status               ON Loans(Status);
CREATE INDEX IX_PayrollRuns_BranchMonthYr  ON PayrollRuns(BranchID, Month, Year);
CREATE INDEX IX_PayrollDetails_RunID       ON PayrollDetails(PayrollRunID);
CREATE INDEX IX_PayrollDetails_EmpID       ON PayrollDetails(EmployeeID);
CREATE INDEX IX_Payslips_EmployeeID        ON Payslips(EmployeeID);
CREATE INDEX IX_Transactions_BranchDate    ON Transactions(BranchID, TransactionDate);
CREATE INDEX IX_RolePermissions_RoleID     ON RolePermissions(RoleID);
CREATE INDEX IX_EmployeeRoles_EmployeeID   ON EmployeeRoles(EmployeeID);
GO

-- =====================================================================
-- TABLES SUMMARY
-- =====================================================================
-- 01. Companies             — Master tenant (signup)
-- 02. Branches              — Company branches
-- 03. Departments           — Per branch departments
-- 04. Roles                 — System + custom roles
-- 05. Employees             — All staff + Head
-- 06. EmployeeRoles         — Employee ko role assign
-- 07. Modules               — System modules/features
-- 08. Permissions           — Actions per module
-- 09. RolePermissions       — Role ke permissions
-- 10. EmployeePermissions   — Individual override permissions
-- 11. SalaryStructures      — Per employee salary setup
-- 12. Attendance            — Daily time-in/out
-- 13. LeaveTypes            — Annual/Sick/Casual etc
-- 14. LeaveApplications     — Leave requests
-- 15. LeaveBalances         — Remaining leave balance
-- 16. Loans                 — Loan/advance requests
-- 17. PayrollRuns           — Monthly payroll run
-- 18. PayrollDetails        — Per employee payroll breakdown
-- 19. PayrollOverrides      — Manual edit tracking
-- 20. Payslips              — Generated payslip records
-- 21. Transactions          — Accounts expense/revenue
-- 22. RefreshTokens         — JWT token management
-- =====================================================================
