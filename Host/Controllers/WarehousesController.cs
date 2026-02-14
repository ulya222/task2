using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelecomProd.Core;

namespace TelecomProd.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehousesController : ControllerBase
{
    private readonly TelecomDbContext _context;
    public WarehousesController(TelecomDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetWarehouses()
    {
        var list = await _context.Warehouses.OrderBy(w => w.Name).ToListAsync();
        return Ok(list);
    }
}
