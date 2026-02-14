using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelecomProd.Core;
using TelecomProd.Core.Entities;

namespace TelecomProd.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComponentsController : ControllerBase
{
    private readonly TelecomDbContext _context;
    public ComponentsController(TelecomDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetComponents([FromQuery] string? search, [FromQuery] string? componentType, [FromQuery] string? sortBy = "name", [FromQuery] bool ascending = true)
    {
        var query = _context.Components.Include(c => c.Supplier).Include(c => c.StockBalances).ThenInclude(sb => sb.Warehouse).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) || c.Code.Contains(search));
        if (!string.IsNullOrWhiteSpace(componentType))
            query = query.Where(c => c.ComponentType.Contains(componentType));
        query = sortBy?.ToLower() == "code" ? (ascending ? query.OrderBy(c => c.Code) : query.OrderByDescending(c => c.Code))
            : (ascending ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name));
        return Ok(await query.ToListAsync());
    }

    [HttpGet("bycode/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var c = await _context.Components.Include(x => x.Supplier).Include(x => x.StockBalances).ThenInclude(sb => sb.Warehouse)
            .FirstOrDefaultAsync(x => x.Code == code);
        return c == null ? NotFound() : Ok(c);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetComponent(int id)
    {
        var c = await _context.Components.Include(x => x.Supplier).Include(x => x.StockBalances).ThenInclude(sb => sb.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id);
        return c == null ? NotFound() : Ok(c);
    }

    [HttpPost]
    public async Task<IActionResult> CreateComponent([FromBody] CreateComponentDto? dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { detail = "Укажите наименование." });
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest(new { detail = "Укажите код (формат TYPE-XXXXX)." });
        if (await _context.Components.AnyAsync(c => c.Code == dto.Code)) return BadRequest(new { detail = "Код уже существует." });
        var expiryUtc = dto.ExpiryDate.HasValue
            ? DateTime.SpecifyKind(dto.ExpiryDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;
        var entity = new Component
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            ComponentType = dto.ComponentType ?? "passive,electronic",
            Manufacturer = dto.Manufacturer ?? "",
            TechSpecsJson = dto.TechSpecsJson,
            UnitOfMeasure = dto.UnitOfMeasure ?? "шт",
            MinStock = dto.MinStock >= 0 ? dto.MinStock : 0,
            MaxStock = dto.MaxStock > 0 ? dto.MaxStock : 1000,
            ExpiryDate = expiryUtc,
            SupplierId = dto.SupplierId > 0 ? dto.SupplierId : null
        };
        _context.Components.Add(entity);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetComponent), new { id = entity.Id }, entity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateComponent(int id, [FromBody] Component comp)
    {
        if (id != comp.Id) return BadRequest();
        var existing = await _context.Components.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Name = comp.Name;
        existing.ComponentType = comp.ComponentType;
        existing.Manufacturer = comp.Manufacturer;
        existing.TechSpecsJson = comp.TechSpecsJson;
        existing.UnitOfMeasure = comp.UnitOfMeasure;
        existing.MinStock = comp.MinStock;
        existing.MaxStock = comp.MaxStock;
        existing.ExpiryDate = comp.ExpiryDate;
        existing.SupplierId = comp.SupplierId > 0 ? comp.SupplierId : null;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComponent(int id)
    {
        var c = await _context.Components.FindAsync(id);
        if (c == null) return NotFound();
        _context.Components.Remove(c);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class CreateComponentDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ComponentType { get; set; }
    public string? Manufacturer { get; set; }
    public string? TechSpecsJson { get; set; }
    public string? UnitOfMeasure { get; set; }
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? SupplierId { get; set; }
}
