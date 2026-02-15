using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataVault.Core;
using DataVault.Core.Entities;

namespace DataVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VerificationsController : ControllerBase
{
    private readonly DataVaultDbContext _context;
    public VerificationsController(DataVaultDbContext context) => _context = context;

    [HttpGet("list")]
    public async Task<IActionResult> GetVerifications([FromQuery] int? taskId)
    {
        var query = _context.Verifications.Include(v => v.WorkTask).ThenInclude(t => t!.Category).AsQueryable();
        if (taskId.HasValue) query = query.Where(v => v.WorkTaskId == taskId.Value);
        var list = await query.OrderByDescending(v => v.VerifiedAt).Take(200).ToListAsync();
        return Ok(list);
    }

    [HttpPost("add")]
    public async Task<IActionResult> CreateVerification([FromBody] VerificationDto? dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.ProcedureName)) return BadRequest();
        var entity = new Verification
        {
            WorkTaskId = dto.WorkTaskId,
            ProcedureName = dto.ProcedureName,
            ResultValue = dto.ResultValue,
            Passed = dto.Passed,
            CertificateNumber = dto.CertificateNumber,
            VerifiedAt = DateTime.UtcNow
        };
        _context.Verifications.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpGet("remarks")]
    public async Task<IActionResult> GetRemarks([FromQuery] string? remarkType)
    {
        var query = _context.Remarks.Include(r => r.WorkTask).ThenInclude(t => t!.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(remarkType)) query = query.Where(r => r.RemarkType.Contains(remarkType));
        var list = await query.OrderByDescending(r => r.RecordedAt).Take(100).ToListAsync();
        return Ok(list);
    }

    [HttpPost("remark")]
    public async Task<IActionResult> CreateRemark([FromBody] RemarkDto? dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.RemarkType)) return BadRequest();
        var entity = new Remark
        {
            WorkTaskId = dto.WorkTaskId,
            RemarkType = dto.RemarkType,
            Description = dto.Description,
            RecordedAt = DateTime.UtcNow
        };
        _context.Remarks.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(entity);
    }
}

public class VerificationDto
{
    public int WorkTaskId { get; set; }
    public string ProcedureName { get; set; } = "";
    public string? ResultValue { get; set; }
    public bool Passed { get; set; }
    public string? CertificateNumber { get; set; }
}

public class RemarkDto
{
    public int WorkTaskId { get; set; }
    public string RemarkType { get; set; } = "";
    public string? Description { get; set; }
}
