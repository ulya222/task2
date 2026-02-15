using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataVault.Core;
using DataVault.Core.Entities;

namespace DataVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly DataVaultDbContext _context;
    public InventoryController(DataVaultDbContext context) => _context = context;

    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances([FromQuery] int? resourceId, [FromQuery] int? storageId)
    {
        var query = _context.ResourceBalances.Include(rb => rb.Resource).Include(rb => rb.Storage).AsQueryable();
        if (resourceId.HasValue) query = query.Where(rb => rb.ResourceId == resourceId.Value);
        if (storageId.HasValue) query = query.Where(rb => rb.StorageId == storageId.Value);
        var list = await query.ToListAsync();
        return Ok(list);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int? resourceId, [FromQuery] int limit = 100)
    {
        var query = _context.ResourceTransactions.Include(rt => rt.Resource).Include(rt => rt.Storage)
            .OrderByDescending(rt => rt.CreatedAt).Take(limit).AsQueryable();
        if (resourceId.HasValue) query = query.Where(rt => rt.ResourceId == resourceId.Value);
        var list = await query.ToListAsync();
        return Ok(list);
    }

    [HttpPost("transaction")]
    public async Task<IActionResult> CreateTransaction([FromBody] TransactionDto? dto)
    {
        if (dto == null) return BadRequest();
        var balance = await _context.ResourceBalances.FirstOrDefaultAsync(rb => rb.ResourceId == dto.ResourceId && rb.StorageId == dto.StorageId);
        if (balance == null)
        {
            balance = new ResourceBalance { ResourceId = dto.ResourceId, StorageId = dto.StorageId, Quantity = 0 };
            _context.ResourceBalances.Add(balance);
            await _context.SaveChangesAsync();
        }
        var qty = dto.Quantity;
        if (dto.TransactionType == "out") qty = -Math.Abs(qty);
        else if (dto.TransactionType == "in") qty = Math.Abs(qty);
        balance.Quantity += qty;
        if (balance.Quantity < 0) return BadRequest(new { detail = "Недостаточно остатков." });
        _context.ResourceTransactions.Add(new ResourceTransaction
        {
            ResourceId = dto.ResourceId, StorageId = dto.StorageId,
            TransactionType = dto.TransactionType ?? "in", Quantity = Math.Abs(dto.Quantity),
            CreatedAt = DateTime.UtcNow, Comment = dto.Comment
        });
        await _context.SaveChangesAsync();
        return Ok(balance);
    }
}

public class TransactionDto
{
    public int ResourceId { get; set; }
    public int StorageId { get; set; }
    public string? TransactionType { get; set; }
    public int Quantity { get; set; }
    public string? Comment { get; set; }
}
