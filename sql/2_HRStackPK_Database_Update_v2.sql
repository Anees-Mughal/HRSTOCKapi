-- =====================================================================
-- HRStackPK — Database Update v2 (run AFTER HRStackPK_Database.sql)
-- Adds columns/tables the API needs. Safe to run multiple times.
-- =====================================================================
USE HRStackPK;
GO

-- 1) Employees: salary basics + blood group (frontend contract fields)
IF COL_LENGTH('Employees', 'BasicSalary') IS NULL
    ALTER TABLE Employees ADD BasicSalary DECIMAL(12,2) NOT NULL DEFAULT 0;
IF COL_LENGTH('Employees', 'Conveyance') IS NULL
    ALTER TABLE Employees ADD Conveyance DECIMAL(12,2) NOT NULL DEFAULT 0;
IF COL_LENGTH('Employees', 'BloodGroup') IS NULL
    ALTER TABLE Employees ADD BloodGroup NVARCHAR(10) NULL;
GO

-- 2) Departments: manager (frontend: {id, name, managerId})
IF COL_LENGTH('Departments', 'ManagerID') IS NULL
BEGIN
    ALTER TABLE Departments ADD ManagerID BIGINT NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Departments_Manager')
    ALTER TABLE Departments ADD CONSTRAINT FK_Departments_Manager
        FOREIGN KEY (ManagerID) REFERENCES Employees(ID);
GO

-- 3) Appraisals (web ERP Appraisals page)
IF OBJECT_ID('Appraisals', 'U') IS NULL
BEGIN
    CREATE TABLE Appraisals (
        ID              BIGINT PRIMARY KEY IDENTITY(1,1),
        CompanyID       BIGINT          NOT NULL,
        BranchID        BIGINT          NOT NULL,
        EmployeeID      BIGINT          NOT NULL,
        Cycle           NVARCHAR(50)    NOT NULL,        -- e.g. '2026-H1'
        Goals           NVARCHAR(1000),
        Rating          INT,
        ManagerComment  NVARCHAR(1000),
        Status          NVARCHAR(20)    DEFAULT 'Draft', -- Draft / Completed
        CreatedAt       DATETIME        DEFAULT GETDATE(),

        CONSTRAINT FK_Appraisals_Companies  FOREIGN KEY (CompanyID)  REFERENCES Companies(ID),
        CONSTRAINT FK_Appraisals_Branches   FOREIGN KEY (BranchID)   REFERENCES Branches(ID),
        CONSTRAINT FK_Appraisals_Employees  FOREIGN KEY (EmployeeID) REFERENCES Employees(ID)
    );
    CREATE INDEX IX_Appraisals_EmployeeID ON Appraisals(EmployeeID);
END
GO

-- 4) EmployeePayrollOverrides — per-employee monthly overrides BEFORE payroll run
--    (frontend: PUT /payroll/overrides/{employeeId} with 8 editable fields + "Reset to auto")
--    Existing PayrollOverrides table stays for post-run audit; this one drives the UI.
IF OBJECT_ID('EmployeePayrollOverrides', 'U') IS NULL
BEGIN
    CREATE TABLE EmployeePayrollOverrides (
        ID              BIGINT PRIMARY KEY IDENTITY(1,1),
        CompanyID       BIGINT          NOT NULL,
        BranchID        BIGINT          NOT NULL,
        EmployeeID      BIGINT          NOT NULL,
        Month           INT             NOT NULL,        -- 1-12
        Year            INT             NOT NULL,
        Basic           DECIMAL(12,2)   NULL,
        HRA             DECIMAL(12,2)   NULL,
        Medical         DECIMAL(12,2)   NULL,
        Conveyance      DECIMAL(12,2)   NULL,
        Tax             DECIMAL(12,2)   NULL,
        EOBI            DECIMAL(12,2)   NULL,
        Loan            DECIMAL(12,2)   NULL,
        LateDeduction   DECIMAL(12,2)   NULL,
        UpdatedAt       DATETIME        DEFAULT GETDATE(),

        CONSTRAINT FK_EmpPayOverrides_Companies FOREIGN KEY (CompanyID)  REFERENCES Companies(ID),
        CONSTRAINT FK_EmpPayOverrides_Branches  FOREIGN KEY (BranchID)   REFERENCES Branches(ID),
        CONSTRAINT FK_EmpPayOverrides_Employees FOREIGN KEY (EmployeeID) REFERENCES Employees(ID),
        CONSTRAINT UQ_EmpPayOverrides           UNIQUE (EmployeeID, Month, Year)
    );
END
GO

PRINT 'Database update v2 applied ✔';
GO
