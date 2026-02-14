using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelecomProd.Core;
using TelecomProd.Core.Entities;

namespace TelecomProd.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly TelecomDbContext _context;
    public StockController(TelecomDbContext context) => _context = context;

    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances([FromQuery] int? componentId, [FromQuery] int? warehouseId)
    {
        var query = _context.StockBalances.Include(sb => sb.Component).Include(sb => sb.Warehouse).AsQueryable();
        if (componentId.HasValue) query = query.Where(sb => sb.ComponentId == componentId.Value);
        if (warehouseId.HasValue) query = query.Where(sb => sb.WarehouseId == warehouseId.Value);
        var list = await query.ToListAsync();
        return Ok(list);
    }

    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements([FromQuery] int? componentId, [FromQuery] int limit = 100)
    {
        var query = _context.StockMovements.Include(sm => sm.Component).Include(sm => sm.Warehouse)
            .OrderByDescending(sm => sm.CreatedAt).Take(limit).AsQueryable();
        if (componentId.HasValue) query = query.Where(sm => sm.ComponentId == componentId.Value);
        var list = await query.ToListAsync();
        return Ok(list);
    }

    [HttpPost("movement")]
    public async Task<IActionResult> CreateMovement([FromBody] StockMovementDto? dto)
    {
        if (dto == null) return BadRequest();
        var balance = await _context.StockBalances.FirstOrDefaultAsync(sb => sb.ComponentId == dto.ComponentId && sb.WarehouseId == dto.WarehouseId);
        if (balance == null)
        {
            balance = new StockBalance { ComponentId = dto.ComponentId, WarehouseId = dto.WarehouseId, Quantity = 0 };
            _context.StockBalances.Add(balance);
            await _context.SaveChangesAsync();
        }
        var qty = dto.Quantity;
        if (dto.MovementType == "out") qty = -Math.Abs(qty);
        else if (dto.MovementType == "in") qty = Math.Abs(qty);
        balance.Quantity += qty;
        if (balance.Quantity < 0) return BadRequest(new { detail = "Недостаточно остатков." });
        _context.StockMovements.Add(new StockMovement
        {
            ComponentId = dto.ComponentId, WarehouseId = dto.WarehouseId,
            MovementType = dto.MovementType ?? "in", Quantity = Math.Abs(dto.Quantity),
            CreatedAt = DateTime.UtcNow, Comment = dto.Comment
        });
        await _context.SaveChangesAsync();
        return Ok(balance);
    }
}

public class StockMovementDto
{
    public int ComponentId { get; set; }
    public int WarehouseId { get; set; }
    public string? MovementType { get; set; }
    public int Quantity { get; set; }
    public string? Comment { get; set; }
}
