using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelecomProd.Core;

namespace TelecomProd.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly TelecomDbContext _context;
    public SuppliersController(TelecomDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetSuppliers()
    {
        var list = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
        return Ok(list);
    }
}
