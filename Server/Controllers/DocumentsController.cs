using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataVault.Core;

namespace DataVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DataVaultDbContext _context;
    public DocumentsController(DataVaultDbContext context) => _context = context;

    [HttpGet("passport/{taskId}")]
    public async Task<IActionResult> GetPassport(int taskId)
    {
        var task = await _context.WorkTasks
            .Include(t => t.Category).Include(t => t.Phase).Include(t => t.User)
            .Include(t => t.Verifications).Include(t => t.Remarks)
            .FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return NotFound();
        var passport = new
        {
            taskId = task.Id,
            category = task.Category?.Name,
            phase = task.Phase?.Name,
            quantity = task.Quantity,
            createdAt = task.CreatedAt,
            plannedFinishAt = task.PlannedFinishAt,
            verificationsCount = task.Verifications?.Count ?? 0,
            remarksCount = task.Remarks?.Count ?? 0
        };
        return Ok(passport);
    }
}
