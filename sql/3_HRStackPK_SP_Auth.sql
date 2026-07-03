-- =====================================================================
-- HRStackPK — Stored Procedures: AUTH MODULE
-- Run AFTER HRStackPK_Database.sql
-- =====================================================================
USE HRStackPK;
GO

-- =====================================================================
-- 1. usp_Auth_Signup
--    Creates: Company + main Branch + per-company LeaveTypes +
--    default RolePermissions (matrix for the 4 system roles).
--    Throws 51001 if mobile already registered.
--    Returns: CompanyID, BranchID
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Auth_Signup
    @CompanyName    NVARCHAR(200),
    @OwnerName      NVARCHAR(200),
    @Email          NVARCHAR(150),
    @Mobile         NVARCHAR(20),
    @PasswordHash   NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM Companies WHERE Mobile = @Mobile)
        THROW 51001, 'Mobile number already registered', 1;

    BEGIN TRANSACTION;

    -- 1) Company (Head account — Mobile is the login identifier)
    INSERT INTO Companies (CompanyName, Email, Mobile, Password)
    VALUES (@CompanyName, @Email, @Mobile, @PasswordHash);

    DECLARE @CompanyID BIGINT = SCOPE_IDENTITY();

    -- 2) Main branch
    INSERT INTO Branches (CompanyID, BranchName)
    VALUES (@CompanyID, N'Main Branch');

    DECLARE @BranchID BIGINT = SCOPE_IDENTITY();

    -- 3) Default leave types (common Pakistani HR policy)
    INSERT INTO LeaveTypes (CompanyID, Name, TotalDays) VALUES
    (@CompanyID, N'Annual', 15),
    (@CompanyID, N'Sick',   10),
    (@CompanyID, N'Casual',  5);

    -- 4) Default RolePermissions matrix (system RoleIDs: 1 Head, 2 HR Manager, 3 Accountant, 4 Staff)
    DECLARE @Matrix TABLE (RoleID BIGINT, ModuleKey NVARCHAR(100), PermissionKey NVARCHAR(100));

    -- ---- Head: everything
    INSERT INTO @Matrix
    SELECT 1, m.ModuleKey, p.PermissionKey
    FROM Permissions p JOIN Modules m ON m.ID = p.ModuleID;

    -- ---- HR Manager
    INSERT INTO @Matrix VALUES
    (2,'dashboard','view'),
    (2,'employees','view'),(2,'employees','add'),(2,'employees','edit'),
    (2,'departments','view'),(2,'departments','add'),(2,'departments','edit'),(2,'departments','delete'),
    (2,'attendance','view'),(2,'attendance','mark'),(2,'attendance','edit'),
    (2,'payroll','view'),
    (2,'payslips','view'),
    (2,'leaves','view'),(2,'leaves','apply'),(2,'leaves','approve'),
    (2,'loans','view'),(2,'loans','approve'),
    (2,'accounts','view'),
    (2,'reports','view'),(2,'reports','export');

    -- ---- Accountant
    INSERT INTO @Matrix VALUES
    (3,'dashboard','view'),
    (3,'employees','view'),
    (3,'departments','view'),
    (3,'attendance','view'),
    (3,'payroll','view'),
    (3,'payslips','view'),
    (3,'leaves','view'),
    (3,'loans','view'),
    (3,'accounts','view'),(3,'accounts','add'),(3,'accounts','edit'),(3,'accounts','delete'),
    (3,'reports','view'),(3,'reports','export');

    -- ---- Staff (own-data scoping is enforced in the API layer)
    INSERT INTO @Matrix VALUES
    (4,'dashboard','view'),
    (4,'attendance','mark'),
    (4,'payroll','view'),
    (4,'payslips','view'),
    (4,'leaves','view'),(4,'leaves','apply'),
    (4,'loans','view'),(4,'loans','apply');

    INSERT INTO RolePermissions (CompanyID, RoleID, PermissionID)
    SELECT @CompanyID, x.RoleID, p.ID
    FROM @Matrix x
    JOIN Modules m     ON m.ModuleKey = x.ModuleKey
    JOIN Permissions p ON p.ModuleID = m.ID AND p.PermissionKey = x.PermissionKey;

    COMMIT TRANSACTION;

    SELECT @CompanyID AS CompanyID, @BranchID AS BranchID;
END
GO

-- =====================================================================
-- 2. usp_Auth_GetHeadByMobile — Head login lookup (Companies table)
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Auth_GetHeadByMobile
    @Mobile NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        c.ID          AS CompanyID,
        c.CompanyName,
        c.Mobile,
        c.Password,
        c.IsActive,
        b.ID          AS BranchID
    FROM Companies c
    CROSS APPLY (
        SELECT TOP 1 ID FROM Branches
        WHERE CompanyID = c.ID AND IsActive = 1
        ORDER BY ID
    ) b
    WHERE c.Mobile = @Mobile;
END
GO

-- =====================================================================
-- 3. usp_Auth_GetHeadByCompanyID — for refresh token flow
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Auth_GetHeadByCompanyID
    @CompanyID BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        c.ID          AS CompanyID,
        c.CompanyName,
        c.Mobile,
        c.Password,
        c.IsActive,
        b.ID          AS BranchID
    FROM Companies c
    CROSS APPLY (
        SELECT TOP 1 ID FROM Branches
        WHERE CompanyID = c.ID AND IsActive = 1
        ORDER BY ID
    ) b
    WHERE c.ID = @CompanyID;
END
GO

-- =====================================================================
-- 4. usp_Auth_GetStaffByEmail — Staff login lookup (Employees table)
--    Role = employee's assigned role (lowest RoleID = highest privilege);
--    falls back to system 'Staff' (RoleID 4) if none assigned.
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Auth_GetStaffByEmail
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        e.ID        AS EmployeeID,
        e.CompanyID,
        e.BranchID,
        e.FullName,
        e.Email,
        e.Password,
        e.IsActive,
        ISNULL(r.ID, 4)              AS RoleID,
        ISNULL(r.RoleName, N'Staff') AS RoleName
    FROM Employees e
    OUTER APPLY (
        SELECT TOP 1 ro.ID, ro.RoleName
        FROM EmployeeRoles er
        JOIN Roles ro ON ro.ID = er.RoleID AND ro.IsActive = 1
        WHERE er.EmployeeID = e.ID
        ORDER BY ro.ID
    ) r
    WHERE e.Email = @Email;
END
GO

-- =====================================================================
-- 5. usp_Auth_GetStaffByID — for refresh token flow
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_Auth_GetStaffByID
    @EmployeeID BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        e.ID        AS EmployeeID,
        e.CompanyID,
        e.BranchID,
        e.FullName,
        e.Email,
        e.Password,
        e.IsActive,
        ISNULL(r.ID, 4)              AS RoleID,
        ISNULL(r.RoleName, N'Staff') AS RoleName
    FROM Employees e
    OUTER APPLY (
        SELECT TOP 1 ro.ID, ro.RoleName
        FROM EmployeeRoles er
        JOIN Roles ro ON ro.ID = er.RoleID AND ro.IsActive = 1
        WHERE er.EmployeeID = e.ID
        ORDER BY ro.ID
    ) r
    WHERE e.ID = @EmployeeID;
END
GO

-- =====================================================================
-- 6. usp_RefreshToken_Save
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_RefreshToken_Save
    @CompanyID   BIGINT,
    @EmployeeID  BIGINT = NULL,
    @IsHeadLogin BIT,
    @Token       NVARCHAR(500),
    @ExpiresAt   DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO RefreshTokens (CompanyID, EmployeeID, IsHeadLogin, Token, ExpiresAt)
    VALUES (@CompanyID, @EmployeeID, @IsHeadLogin, @Token, @ExpiresAt);
END
GO

-- =====================================================================
-- 7. usp_RefreshToken_Get
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_RefreshToken_Get
    @Token NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ID, CompanyID, EmployeeID, IsHeadLogin, Token, ExpiresAt, IsRevoked
    FROM RefreshTokens
    WHERE Token = @Token;
END
GO

-- =====================================================================
-- 8. usp_RefreshToken_Revoke
-- =====================================================================
CREATE OR ALTER PROCEDURE usp_RefreshToken_Revoke
    @Token NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE RefreshTokens SET IsRevoked = 1 WHERE Token = @Token;
END
GO

PRINT 'Auth stored procedures created ✔';
GO
