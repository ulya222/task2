using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelecomProd.Core;

namespace TelecomProd.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderStatusesController : ControllerBase
{
    private readonly TelecomDbContext _context;
    public OrderStatusesController(TelecomDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetStatuses()
    {
        var list = await _context.OrderStatuses.OrderBy(s => s.Id).ToListAsync();
        return Ok(list);
    }
}
