# HRStackPK — ASP.NET Core 8 Web API (Complete)

Visual Studio 2022 solution. Covers **all web ERP + mobile app APIs** — auth, employees, departments, attendance (incl. mobile check-in), leaves + balances, loans, payroll overrides + generate, payslips, accounts (expenses / revenues / monthly report), appraisals.

Pattern: **Controller → DAL → Stored Procedure** (ADO.NET, no EF Core) — SchoolMentor style.

---

## 1. Database setup (SSMS — run in this exact order)

| # | File | What it does |
|---|------|--------------|
| 1 | `sql/1_HRStackPK_Database.sql` | Base 22 tables (**you already ran this — skip**) |
| 2 | `sql/2_HRStackPK_Database_Update_v2.sql` | **REQUIRED CHANGES**: adds `BasicSalary`, `Conveyance`, `BloodGroup` to Employees; `ManagerID` to Departments; new `Appraisals` + `EmployeePayrollOverrides` tables. Safe to run twice. |
| 3 | `sql/3_HRStackPK_SP_Auth.sql` | 8 auth stored procedures |
| 4 | `sql/4_HRStackPK_SP_Modules.sql` | 33 stored procedures — all modules |

## 2. Run the API (Visual Studio 2022)

1. Open **`HRStackPK.sln`**
2. `appsettings.json` → check connection string (default `Server=.;Database=HRStackPK;Trusted_Connection=True;TrustServerCertificate=True`). SQL auth ho to: `Server=.;Database=HRStackPK;User Id=sa;Password=YourPass;TrustServerCertificate=True`
3. Press **F5** → browser opens **Swagger** at `http://localhost:5088/swagger`

NuGet packages restore automatically on first build (internet needed once).

## 3. Test flow in Swagger

1. `POST /api/auth/signup` → creates company + Head + Main Branch + leave types + role permissions
2. `POST /api/auth/login` → `{ "identifier": "0300-1234567", "password": "..." }` (mobile = Head, email = staff)
3. Copy `accessToken` → click **Authorize** button (top-right) → paste → all APIs unlocked
4. Create department → create employee (staff default password **`123456`**) → mark attendance → apply/approve leave → payroll generate → payslips → accounts report

## 4. Endpoints (web + mobile — same API)

- **Auth**: `POST /api/auth/signup · login · refresh · logout`
- **Employees**: `GET|POST /api/employees` · `GET /api/employees/me` · `GET|PUT|DELETE /api/employees/{id}` (soft delete)
- **Departments**: `GET|POST /api/departments` · `PUT /api/departments/{id}`
- **Attendance**: `GET /api/attendance?date=` · `POST /api/attendance/mark` (upsert; Absent clears times) · `GET /api/attendance/me?from=&to=` · `GET /api/attendance/employee/{id}` · **`POST /api/attendance/check-in`** (mobile; Late after 09:15)
- **Leaves**: `GET|POST /api/leaves` · `GET /api/leaves/types` · `GET /api/leaves/me` · `GET /api/leaves/me/balances` · `PUT /api/leaves/{id}/status` (approve updates balances)
- **Loans**: `GET|POST /api/loans` · `GET /api/loans/me` · `PUT /api/loans/{id}/status`
- **Payroll**: `GET /api/payroll/overrides?month=YYYY-MM` · `PUT /api/payroll/overrides/{employeeId}` (all fields null = reset to auto) · `POST /api/payroll/generate` — **the ONLY payslip creator**; also writes salary expense transactions + decrements approved loans (Cleared at 0); 409 if month already generated
- **Payslips**: `GET /api/payslips` · `/me` · `/employee/{id}` · `/overview?month=`
- **Accounts**: `GET|POST /api/expenses` · `GET|POST /api/revenues` · `GET /api/accounts/report?month=` (expenses groups with salaries first, revenue groups, P&L, salary count)
- **Appraisals**: `GET|POST /api/appraisals`

Tenant (CompanyID/BranchID/EmployeeID) is always read **from the JWT** — never from the client.

## 5. Connect the frontends

**Web (React):** `.env` → `VITE_USE_MOCK=0` and `VITE_API_URL=http://localhost:5088/api`

**Mobile (React Native):** `src/api/config.js` → `API_URL = "http://10.0.2.2:5088/api"` (Android emulator) or PC LAN IP for real device (e.g. `http://192.168.1.5:5088/api`).

## 6. Notes

- JWT: access 60 min, refresh 30 days (rotated on refresh). Change `Jwt:Key` in `appsettings.json` before production.
- New staff default password: **123456** (BCrypt-hashed).
- `EmployeePayrollOverrides` (new table) drives the pre-run overrides UI; old `PayrollOverrides` table remains for post-run audit.
