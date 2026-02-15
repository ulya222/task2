using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataVault.Core;

namespace DataVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskPhasesController : ControllerBase
{
    private readonly DataVaultDbContext _context;
    public TaskPhasesController(DataVaultDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetPhases()
    {
        var list = await _context.TaskPhases.OrderBy(p => p.Id).ToListAsync();
        return Ok(list);
    }
}
