using System.Data;
using Microsoft.Data.SqlClient;

namespace HRStackPK.Api.DAL;

/// <summary>
/// Thin ADO.NET helper — every DB call goes through a stored procedure.
/// Controller → DAL → SP (SchoolMentor pattern, no EF Core).
/// </summary>
public class Db
{
    private readonly string _cs;

    public Db(IConfiguration cfg)
    {
        _cs = cfg.GetConnectionString("HRStackPK")
              ?? throw new InvalidOperationException("Connection string 'HRStackPK' missing");
    }

    public SqlConnection Open()
    {
        var con = new SqlConnection(_cs);
        con.Open();
        return con;
    }

    public static SqlCommand Sp(SqlConnection con, string procName, params (string name, object? value)[] args)
    {
        var cmd = new SqlCommand(procName, con) { CommandType = CommandType.StoredProcedure };
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }

    // ------- reader helpers -------
    public static long   L (SqlDataReader r, string col) => Convert.ToInt64(r[col]);
    public static long?  Ln(SqlDataReader r, string col) => r[col] == DBNull.Value ? null : Convert.ToInt64(r[col]);
    public static string S (SqlDataReader r, string col) => r[col] == DBNull.Value ? "" : Convert.ToString(r[col])!;
    public static bool   B (SqlDataReader r, string col) => r[col] != DBNull.Value && Convert.ToBoolean(r[col]);
    public static DateTime D(SqlDataReader r, string col) => Convert.ToDateTime(r[col]);
    public static decimal Dec(SqlDataReader r, string col) => r[col] == DBNull.Value ? 0 : Convert.ToDecimal(r[col]);
    public static decimal? DecN(SqlDataReader r, string col) => r[col] == DBNull.Value ? null : Convert.ToDecimal(r[col]);
    public static int    I (SqlDataReader r, string col) => r[col] == DBNull.Value ? 0 : Convert.ToInt32(r[col]);
    public static int?   In(SqlDataReader r, string col) => r[col] == DBNull.Value ? null : Convert.ToInt32(r[col]);

    /// <summary>DATE/DATETIME column → "yyyy-MM-dd" ("" if NULL).</summary>
    public static string DateS(SqlDataReader r, string col) =>
        r[col] == DBNull.Value ? "" : Convert.ToDateTime(r[col]).ToString("yyyy-MM-dd");

    /// <summary>TIME column → "HH:mm" (null if NULL).</summary>
    public static string? TimeS(SqlDataReader r, string col) =>
        r[col] == DBNull.Value ? null : ((TimeSpan)r[col]).ToString(@"hh\:mm");
}
