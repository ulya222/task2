using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataVault.Core;
using DataVault.Core.Entities;

namespace DataVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly DataVaultDbContext _context;
    public CategoriesController(DataVaultDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var list = await _context.Categories.OrderBy(c => c.Code).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}/items")]
    public async Task<IActionResult> GetCategoryItems(int id)
    {
        var items = await _context.CategoryItems.Include(ci => ci.Resource).Where(ci => ci.CategoryId == id).ToListAsync();
        return Ok(items);
    }

    [HttpPost("{id}/items")]
    public async Task<IActionResult> AddCategoryItem(int id, [FromBody] CategoryItemDto? dto)
    {
        if (dto == null || dto.ResourceId <= 0) return BadRequest();
        var exists = await _context.CategoryItems.AnyAsync(ci => ci.CategoryId == id && ci.ResourceId == dto.ResourceId);
        if (exists) return BadRequest(new { detail = "Ресурс уже в составе категории." });
        var item = new CategoryItem { CategoryId = id, ResourceId = dto.ResourceId, Quantity = dto.Quantity > 0 ? dto.Quantity : 1 };
        _context.CategoryItems.Add(item);
        await _context.SaveChangesAsync();
        await _context.Entry(item).Reference(ci => ci.Resource).LoadAsync();
        return Ok(item);
    }

    [HttpPut("items/{itemId}")]
    public async Task<IActionResult> UpdateCategoryItem(int itemId, [FromBody] CategoryItemDto? dto)
    {
        if (dto == null) return BadRequest();
        var item = await _context.CategoryItems.FindAsync(itemId);
        if (item == null) return NotFound();
        item.Quantity = dto.Quantity > 0 ? dto.Quantity : 1;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("items/{itemId}")]
    public async Task<IActionResult> DeleteCategoryItem(int itemId)
    {
        var item = await _context.CategoryItems.FindAsync(itemId);
        if (item == null) return NotFound();
        _context.CategoryItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class CategoryItemDto
{
    public int ResourceId { get; set; }
    public int Quantity { get; set; }
}
