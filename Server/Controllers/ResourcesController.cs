using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataVault.Core;
using DataVault.Core.Entities;

namespace DataVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResourcesController : ControllerBase
{
    private readonly DataVaultDbContext _context;
    public ResourcesController(DataVaultDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetResources([FromQuery] string? search, [FromQuery] string? resourceKind, [FromQuery] string? sortBy = "name", [FromQuery] bool ascending = true)
    {
        var query = _context.Resources.Include(r => r.Vendor).Include(r => r.ResourceBalances).ThenInclude(rb => rb.Storage).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Name.Contains(search) || r.Code.Contains(search));
        if (!string.IsNullOrWhiteSpace(resourceKind))
            query = query.Where(r => r.ResourceKind.Contains(resourceKind));
        query = sortBy?.ToLower() == "code" ? (ascending ? query.OrderBy(r => r.Code) : query.OrderByDescending(r => r.Code))
            : (ascending ? query.OrderBy(r => r.Name) : query.OrderByDescending(r => r.Name));
        return Ok(await query.ToListAsync());
    }

    [HttpGet("bycode/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var r = await _context.Resources.Include(x => x.Vendor).Include(x => x.ResourceBalances).ThenInclude(rb => rb.Storage)
            .FirstOrDefaultAsync(x => x.Code == code);
        return r == null ? NotFound() : Ok(r);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetResource(int id)
    {
        var r = await _context.Resources.Include(x => x.Vendor).Include(x => x.ResourceBalances).ThenInclude(rb => rb.Storage)
            .FirstOrDefaultAsync(x => x.Id == id);
        return r == null ? NotFound() : Ok(r);
    }

    [HttpPost]
    public async Task<IActionResult> CreateResource([FromBody] CreateResourceDto? dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { detail = "Укажите наименование." });
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest(new { detail = "Укажите код." });
        if (await _context.Resources.AnyAsync(r => r.Code == dto.Code)) return BadRequest(new { detail = "Код уже существует." });
        var expiryUtc = dto.ExpiryDate.HasValue
            ? DateTime.SpecifyKind(dto.ExpiryDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;
        var entity = new Resource
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            ResourceKind = dto.ResourceKind ?? "material",
            Manufacturer = dto.Manufacturer ?? "",
            SpecsJson = dto.SpecsJson,
            UnitOfMeasure = dto.UnitOfMeasure ?? "шт",
            MinStock = dto.MinStock >= 0 ? dto.MinStock : 0,
            MaxStock = dto.MaxStock > 0 ? dto.MaxStock : 1000,
            ExpiryDate = expiryUtc,
            VendorId = dto.VendorId > 0 ? dto.VendorId : null
        };
        _context.Resources.Add(entity);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetResource), new { id = entity.Id }, entity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateResource(int id, [FromBody] Resource resource)
    {
        if (id != resource.Id) return BadRequest();
        var existing = await _context.Resources.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Name = resource.Name;
        existing.ResourceKind = resource.ResourceKind;
        existing.Manufacturer = resource.Manufacturer;
        existing.SpecsJson = resource.SpecsJson;
        existing.UnitOfMeasure = resource.UnitOfMeasure;
        existing.MinStock = resource.MinStock;
        existing.MaxStock = resource.MaxStock;
        existing.ExpiryDate = resource.ExpiryDate;
        existing.VendorId = resource.VendorId > 0 ? resource.VendorId : null;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteResource(int id)
    {
        var r = await _context.Resources.FindAsync(id);
        if (r == null) return NotFound();
        _context.Resources.Remove(r);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class CreateResourceDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ResourceKind { get; set; }
    public string? Manufacturer { get; set; }
    public string? SpecsJson { get; set; }
    public string? UnitOfMeasure { get; set; }
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? VendorId { get; set; }
}
