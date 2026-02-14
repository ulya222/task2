using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelecomProd.Core;
using TelecomProd.Core.Entities;

namespace TelecomProd.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductionOrdersController : ControllerBase
{
    private readonly TelecomDbContext _context;
    public ProductionOrdersController(TelecomDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int? statusId, [FromQuery] int limit = 100)
    {
        var query = _context.ProductionOrders
            .Include(o => o.AssemblyUnit).Include(o => o.Status).Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt).Take(limit).AsQueryable();
        if (statusId.HasValue) query = query.Where(o => o.StatusId == statusId.Value);
        var list = await query.ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var o = await _context.ProductionOrders
            .Include(x => x.AssemblyUnit).Include(x => x.Status).Include(x => x.User)
            .Include(x => x.QualityTests).Include(x => x.DefectRecords)
            .FirstOrDefaultAsync(x => x.Id == id);
        return o == null ? NotFound() : Ok(o);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto? dto)
    {
        if (dto == null || dto.AssemblyUnitId <= 0) return BadRequest();
        var statusNew = await _context.OrderStatuses.FirstOrDefaultAsync(s => s.Name == "Новый");
        var plannedUtc = dto.PlannedFinishAt.HasValue
            ? DateTime.SpecifyKind(dto.PlannedFinishAt.Value, DateTimeKind.Utc)
            : (DateTime?)null;
        var entity = new ProductionOrder
        {
            AssemblyUnitId = dto.AssemblyUnitId,
            StatusId = statusNew?.Id ?? 1,
            UserId = dto.UserId > 0 ? dto.UserId : null,
            Quantity = dto.Quantity > 0 ? dto.Quantity : 1,
            CreatedAt = DateTime.UtcNow,
            PlannedFinishAt = plannedUtc,
            UnitCost = dto.UnitCost,
            AssemblyTimeMinutes = dto.AssemblyTimeMinutes
        };
        _context.ProductionOrders.Add(entity);
        await _context.SaveChangesAsync();
        var created = await _context.ProductionOrders.Include(o => o.AssemblyUnit).Include(o => o.Status).FirstOrDefaultAsync(o => o.Id == entity.Id);
        return CreatedAtAction(nameof(GetOrder), new { id = entity.Id }, created ?? entity);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusUpdateDto? dto)
    {
        if (dto == null) return BadRequest();
        var order = await _context.ProductionOrders.FindAsync(id);
        if (order == null) return NotFound();
        order.StatusId = dto.StatusId;
        if (dto.StatusId == 6) order.ActualFinishAt = DateTime.UtcNow; // Готов
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class CreateOrderDto
{
    public int AssemblyUnitId { get; set; }
    public int? UserId { get; set; }
    public int Quantity { get; set; }
    public DateTime? PlannedFinishAt { get; set; }
    public decimal UnitCost { get; set; }
    public int AssemblyTimeMinutes { get; set; }
}

public class StatusUpdateDto { public int StatusId { get; set; } }
