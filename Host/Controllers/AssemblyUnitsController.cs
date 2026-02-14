using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelecomProd.Core;
using TelecomProd.Core.Entities;

namespace TelecomProd.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssemblyUnitsController : ControllerBase
{
    private readonly TelecomDbContext _context;
    public AssemblyUnitsController(TelecomDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAssemblyUnits()
    {
        var list = await _context.AssemblyUnits.OrderBy(a => a.Code).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}/bom")]
    public async Task<IActionResult> GetBom(int id)
    {
        var items = await _context.BomItems.Include(b => b.Component).Where(b => b.AssemblyUnitId == id).ToListAsync();
        return Ok(items);
    }

    [HttpPost("{id}/bom")]
    public async Task<IActionResult> AddBomItem(int id, [FromBody] BomItemDto? dto)
    {
        if (dto == null || dto.ComponentId <= 0) return BadRequest();
        var exists = await _context.BomItems.AnyAsync(b => b.AssemblyUnitId == id && b.ComponentId == dto.ComponentId);
        if (exists) return BadRequest(new { detail = "Компонент уже в спецификации." });
        var item = new BomItem { AssemblyUnitId = id, ComponentId = dto.ComponentId, Quantity = dto.Quantity > 0 ? dto.Quantity : 1 };
        _context.BomItems.Add(item);
        await _context.SaveChangesAsync();
        await _context.Entry(item).Reference(b => b.Component).LoadAsync();
        return Ok(item);
    }

    [HttpPut("bom/{bomItemId}")]
    public async Task<IActionResult> UpdateBomItem(int bomItemId, [FromBody] BomItemDto? dto)
    {
        if (dto == null) return BadRequest();
        var item = await _context.BomItems.FindAsync(bomItemId);
        if (item == null) return NotFound();
        item.Quantity = dto.Quantity > 0 ? dto.Quantity : 1;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("bom/{bomItemId}")]
    public async Task<IActionResult> DeleteBomItem(int bomItemId)
    {
        var item = await _context.BomItems.FindAsync(bomItemId);
        if (item == null) return NotFound();
        _context.BomItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class BomItemDto
{
    public int ComponentId { get; set; }
    public int Quantity { get; set; }
}
