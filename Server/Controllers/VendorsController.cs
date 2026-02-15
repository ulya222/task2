using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataVault.Core;

namespace DataVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendorsController : ControllerBase
{
    private readonly DataVaultDbContext _context;
    public VendorsController(DataVaultDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetVendors()
    {
        var list = await _context.Vendors.OrderBy(v => v.Name).ToListAsync();
        return Ok(list);
    }
}
