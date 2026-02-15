using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataVault.Core;
using DataVault.Core.Entities;

namespace DataVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkTasksController : ControllerBase
{
    private readonly DataVaultDbContext _context;
    public WorkTasksController(DataVaultDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] int? phaseId, [FromQuery] int limit = 100)
    {
        var query = _context.WorkTasks
            .Include(t => t.Category).Include(t => t.Phase).Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt).Take(limit).AsQueryable();
        if (phaseId.HasValue) query = query.Where(t => t.PhaseId == phaseId.Value);
        var list = await query.ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(int id)
    {
        var t = await _context.WorkTasks
            .Include(x => x.Category).Include(x => x.Phase).Include(x => x.User)
            .Include(x => x.Verifications).Include(x => x.Remarks)
            .FirstOrDefaultAsync(x => x.Id == id);
        return t == null ? NotFound() : Ok(t);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto? dto)
    {
        if (dto == null || dto.CategoryId <= 0) return BadRequest();
        var phaseNew = await _context.TaskPhases.FirstOrDefaultAsync(p => p.Name == "Новый");
        var plannedUtc = dto.PlannedFinishAt.HasValue
            ? DateTime.SpecifyKind(dto.PlannedFinishAt.Value, DateTimeKind.Utc)
            : (DateTime?)null;
        var entity = new WorkTask
        {
            CategoryId = dto.CategoryId,
            PhaseId = phaseNew?.Id ?? 1,
            UserId = dto.UserId > 0 ? dto.UserId : null,
            Quantity = dto.Quantity > 0 ? dto.Quantity : 1,
            CreatedAt = DateTime.UtcNow,
            PlannedFinishAt = plannedUtc,
            UnitCost = dto.UnitCost,
            EstimatedMinutes = dto.EstimatedMinutes
        };
        _context.WorkTasks.Add(entity);
        await _context.SaveChangesAsync();
        var created = await _context.WorkTasks.Include(t => t.Category).Include(t => t.Phase).FirstOrDefaultAsync(t => t.Id == entity.Id);
        return CreatedAtAction(nameof(GetTask), new { id = entity.Id }, created ?? entity);
    }

    [HttpPut("{id}/phase")]
    public async Task<IActionResult> UpdatePhase(int id, [FromBody] PhaseUpdateDto? dto)
    {
        if (dto == null) return BadRequest();
        var task = await _context.WorkTasks.FindAsync(id);
        if (task == null) return NotFound();
        task.PhaseId = dto.PhaseId;
        if (dto.PhaseId == 6) task.ActualFinishAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class CreateTaskDto
{
    public int CategoryId { get; set; }
    public int? UserId { get; set; }
    public int Quantity { get; set; }
    public DateTime? PlannedFinishAt { get; set; }
    public decimal UnitCost { get; set; }
    public int EstimatedMinutes { get; set; }
}

public class PhaseUpdateDto { public int PhaseId { get; set; } }
