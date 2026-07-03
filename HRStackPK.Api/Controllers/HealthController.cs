using HRStackPK.Api.DAL;
using Microsoft.AspNetCore.Mvc;

namespace HRStackPK.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly Db _db;

    public HealthController(Db db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { ok = true, app = "HRStackPK.Api", time = DateTime.UtcNow });
    }

    [HttpGet("db")]
    public IActionResult Db()
    {
        try
        {
            using var con = _db.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT 1";
            var result = cmd.ExecuteScalar();
            return Ok(new { ok = true, database = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                ok = false,
                error = ex.GetType().Name,
                message = ex.Message
            });
        }
    }
}
