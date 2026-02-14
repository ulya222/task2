using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelecomProd.Core;
using TelecomProd.Core.Entities;

namespace TelecomProd.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QualityController : ControllerBase
{
    private readonly TelecomDbContext _context;
    public QualityController(TelecomDbContext context) => _context = context;

    [HttpGet("tests")]
    public async Task<IActionResult> GetTests([FromQuery] int? orderId)
    {
        var query = _context.QualityTests.Include(q => q.ProductionOrder).ThenInclude(o => o!.AssemblyUnit).AsQueryable();
        if (orderId.HasValue) query = query.Where(q => q.ProductionOrderId == orderId.Value);
        var list = await query.OrderByDescending(q => q.TestedAt).Take(200).ToListAsync();
        return Ok(list);
    }

    [HttpPost("test")]
    public async Task<IActionResult> CreateTest([FromBody] QualityTestDto? dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.TestProcedure)) return BadRequest();
        var entity = new QualityTest
        {
            ProductionOrderId = dto.ProductionOrderId,
            TestProcedure = dto.TestProcedure,
            MeasurementResult = dto.MeasurementResult,
            Passed = dto.Passed,
            CertificateNumber = dto.CertificateNumber,
            TestedAt = DateTime.UtcNow
        };
        _context.QualityTests.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpGet("defects")]
    public async Task<IActionResult> GetDefects([FromQuery] string? defectType)
    {
        var query = _context.DefectRecords.Include(d => d.ProductionOrder).ThenInclude(o => o!.AssemblyUnit).AsQueryable();
        if (!string.IsNullOrWhiteSpace(defectType)) query = query.Where(d => d.DefectType.Contains(defectType));
        var list = await query.OrderByDescending(d => d.RecordedAt).Take(100).ToListAsync();
        return Ok(list);
    }

    [HttpPost("defect")]
    public async Task<IActionResult> CreateDefect([FromBody] DefectDto? dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.DefectType)) return BadRequest();
        var entity = new DefectRecord
        {
            ProductionOrderId = dto.ProductionOrderId,
            DefectType = dto.DefectType,
            Description = dto.Description,
            RecordedAt = DateTime.UtcNow
        };
        _context.DefectRecords.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(entity);
    }
}

public class QualityTestDto
{
    public int ProductionOrderId { get; set; }
    public string TestProcedure { get; set; } = "";
    public string? MeasurementResult { get; set; }
    public bool Passed { get; set; }
    public string? CertificateNumber { get; set; }
}

public class DefectDto
{
    public int ProductionOrderId { get; set; }
    public string DefectType { get; set; } = "";
    public string? Description { get; set; }
}
