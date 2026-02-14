using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using TelecomProd.Core;

namespace TelecomProd.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly TelecomDbContext _context;

    public DocumentsController(TelecomDbContext context) => _context = context;

    [HttpGet("qr/{data}")]
    public IActionResult GetQrCode([FromRoute] string data)
    {
        if (string.IsNullOrEmpty(data)) return BadRequest();
        using var qr = new QRCodeGenerator();
        var qrData = qr.CreateQrCode(data ?? "", QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var bytes = qrCode.GetGraphic(4);
        return File(bytes, "image/png");
    }

    [HttpGet("passport/{orderId}")]
    public async Task<IActionResult> GetProductPassport(int orderId)
    {
        var order = await _context.ProductionOrders
            .Include(o => o.AssemblyUnit).Include(o => o.Status)
            .Include(o => o.User).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound();
        var bom = await _context.BomItems.Include(b => b.Component)
            .Where(b => b.AssemblyUnitId == order.AssemblyUnitId).ToListAsync();
        var tests = await _context.QualityTests.Where(q => q.ProductionOrderId == orderId).ToListAsync();
        var passport = new
        {
            orderId = order.Id,
            assemblyUnit = order.AssemblyUnit?.Name,
            code = order.AssemblyUnit?.Code,
            status = order.Status?.Name,
            quantity = order.Quantity,
            createdAt = order.CreatedAt,
            plannedFinish = order.PlannedFinishAt,
            unitCost = order.UnitCost,
            bomItems = bom.Select(b => new { component = b.Component?.Name, code = b.Component?.Code, quantity = b.Quantity }),
            qualityTests = tests.Select(t => new { t.TestProcedure, t.MeasurementResult, t.Passed, t.CertificateNumber })
        };
        return Ok(passport);
    }
}
