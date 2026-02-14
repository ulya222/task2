using Microsoft.AspNetCore.Mvc;
using TelecomProd.Core;

namespace TelecomProd.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly TelecomDbContext _context;
    public HealthController(TelecomDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            await _context.Database.CanConnectAsync();
            return Ok(new { status = "ok", db = "connected" });
        }
        catch
        {
            return StatusCode(503, new { status = "error", db = "disconnected" });
        }
    }
}
