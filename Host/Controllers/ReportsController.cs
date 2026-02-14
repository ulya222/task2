using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelecomProd.Core;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TelecomProd.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly TelecomDbContext _context;
    public ReportsController(TelecomDbContext context) => _context = context;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var today = DateTime.UtcNow.Date;
        var ordersToday = await _context.ProductionOrders.CountAsync(o => o.CreatedAt.Date == today);
        var ordersInProgress = await _context.ProductionOrders.CountAsync(o => o.StatusId >= 2 && o.StatusId <= 5);
        var lowStock = await _context.StockBalances
            .Include(sb => sb.Component)
            .Where(sb => sb.Quantity <= sb.Component.MinStock && sb.Quantity >= 0)
            .Select(sb => new LowStockItemDto { Code = sb.Component!.Code, Name = sb.Component.Name, Quantity = sb.Quantity, MinStock = sb.Component.MinStock })
            .Take(20).ToListAsync();
        var urgentOrders = await _context.ProductionOrders
            .Include(o => o.AssemblyUnit).Include(o => o.Status)
            .Where(o => o.StatusId < 6 && o.PlannedFinishAt.HasValue && o.PlannedFinishAt <= today.AddDays(2))
            .OrderBy(o => o.PlannedFinishAt).Take(10)
            .Select(o => new UrgentOrderDto { Id = o.Id, AssemblyUnit = o.AssemblyUnit!.Name, Status = o.Status!.Name, PlannedFinishAt = o.PlannedFinishAt })
            .ToListAsync();
        return Ok(new DashboardDto { OrdersToday = ordersToday, OrdersInProgress = ordersInProgress, LowStock = lowStock, UrgentOrders = urgentOrders });
    }

    [HttpGet("production")]
    public async Task<IActionResult> GetProductionReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDt = from ?? DateTime.UtcNow.AddDays(-30);
        var toDt = to ?? DateTime.UtcNow;
        var orders = await _context.ProductionOrders
            .Include(o => o.AssemblyUnit).Include(o => o.Status)
            .Where(o => o.CreatedAt >= fromDt && o.CreatedAt <= toDt)
            .ToListAsync();
        var byStatus = orders.GroupBy(o => o.Status.Name).Select(g => new { status = g.Key, count = g.Count() }).ToList();
        return Ok(new { from = fromDt, to = toDt, total = orders.Count, byStatus });
    }

    [HttpGet("quality")]
    public async Task<IActionResult> GetQualityReport()
    {
        var total = await _context.QualityTests.CountAsync();
        var passed = await _context.QualityTests.CountAsync(q => q.Passed);
        var defects = await _context.DefectRecords.GroupBy(d => d.DefectType)
            .Select(g => new { defectType = g.Key, count = g.Count() }).ToListAsync();
        return Ok(new { totalTests = total, passed, defects });
    }

    [HttpGet("export/components")]
    public async Task<IActionResult> ExportComponents([FromQuery] string? search)
    {
        var query = _context.Components.Include(c => c.Supplier).Include(c => c.StockBalances).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) || c.Code.Contains(search));
        var list = await query.OrderBy(c => c.Code).ToListAsync();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Компоненты");
        ws.Cell(1, 1).Value = "Код"; ws.Cell(1, 2).Value = "Наименование"; ws.Cell(1, 3).Value = "Тип";
        ws.Cell(1, 4).Value = "Производитель"; ws.Cell(1, 5).Value = "Остаток"; ws.Cell(1, 6).Value = "Мин"; ws.Cell(1, 7).Value = "Макс";
        int row = 2;
        foreach (var c in list)
        {
            var totalStock = c.StockBalances?.Sum(sb => sb.Quantity) ?? 0;
            ws.Cell(row, 1).Value = c.Code; ws.Cell(row, 2).Value = c.Name; ws.Cell(row, 3).Value = c.ComponentType;
            ws.Cell(row, 4).Value = c.Manufacturer; ws.Cell(row, 5).Value = totalStock; ws.Cell(row, 6).Value = c.MinStock; ws.Cell(row, 7).Value = c.MaxStock;
            row++;
        }
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "components.xlsx");
    }

    [HttpGet("materials-requirement")]
    public async Task<IActionResult> GetMaterialsRequirement([FromQuery] int assemblyUnitId, [FromQuery] int quantity = 1)
    {
        var bom = await _context.BomItems.Include(b => b.Component)
            .Where(b => b.AssemblyUnitId == assemblyUnitId).ToListAsync();
        var requirement = bom.Select(b => new { componentCode = b.Component!.Code, componentName = b.Component.Name, quantity = b.Quantity * quantity }).ToList();
        return Ok(requirement);
    }

    [HttpGet("expiring-components")]
    public async Task<IActionResult> GetExpiringComponents([FromQuery] int days = 90)
    {
        var threshold = DateTime.UtcNow.AddDays(days);
        var list = await _context.Components
            .Where(c => c.ExpiryDate.HasValue && c.ExpiryDate <= threshold)
            .Select(c => new { c.Code, c.Name, c.ExpiryDate })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("export/orders")]
    public async Task<IActionResult> ExportOrders([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDt = from ?? DateTime.UtcNow.AddDays(-30);
        var toDt = to ?? DateTime.UtcNow;
        var list = await _context.ProductionOrders
            .Include(o => o.AssemblyUnit).Include(o => o.Status).Include(o => o.User)
            .Where(o => o.CreatedAt >= fromDt && o.CreatedAt <= toDt)
            .OrderByDescending(o => o.CreatedAt).ToListAsync();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Заказы");
        ws.Cell(1, 1).Value = "№"; ws.Cell(1, 2).Value = "Узел"; ws.Cell(1, 3).Value = "Статус";
        ws.Cell(1, 4).Value = "Кол-во"; ws.Cell(1, 5).Value = "Дата"; ws.Cell(1, 6).Value = "Себестоимость";
        int row = 2;
        foreach (var o in list)
        {
            ws.Cell(row, 1).Value = o.Id; ws.Cell(row, 2).Value = o.AssemblyUnit?.Name; ws.Cell(row, 3).Value = o.Status?.Name;
            ws.Cell(row, 4).Value = o.Quantity; ws.Cell(row, 5).Value = o.CreatedAt; ws.Cell(row, 6).Value = o.UnitCost;
            row++;
        }
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "orders.xlsx");
    }

    [HttpGet("export/dashboard-pdf")]
    public async Task<IActionResult> ExportDashboardPdf()
    {
        var today = DateTime.UtcNow.Date;
        var ordersToday = await _context.ProductionOrders.CountAsync(o => o.CreatedAt.Date == today);
        var ordersInProgress = await _context.ProductionOrders.CountAsync(o => o.StatusId >= 2 && o.StatusId <= 5);
        var lowCount = await _context.StockBalances.Include(sb => sb.Component)
            .CountAsync(sb => sb.Quantity <= sb.Component.MinStock);
        QuestPDF.Settings.License = LicenseType.Community;
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.Header().Text("Отчёт TelecomProd").Bold().FontSize(18);
                page.Content().Column(col =>
                {
                    col.Item().Text($"Дата: {DateTime.UtcNow:dd.MM.yyyy HH:mm}").FontSize(10);
                    col.Item().PaddingVertical(10).Text($"Заказов сегодня: {ordersToday}").FontSize(12);
                    col.Item().Text($"В производстве: {ordersInProgress}").FontSize(12);
                    col.Item().Text($"Позиций с низким остатком: {lowCount}").FontSize(12);
                });
            });
        });
        var stream = new MemoryStream();
        doc.GeneratePdf(stream);
        stream.Position = 0;
        return File(stream.ToArray(), "application/pdf", "dashboard.pdf");
    }
}

public class DashboardDto
{
    public int OrdersToday { get; set; }
    public int OrdersInProgress { get; set; }
    public List<LowStockItemDto> LowStock { get; set; } = new();
    public List<UrgentOrderDto> UrgentOrders { get; set; } = new();
}

public class LowStockItemDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public int MinStock { get; set; }
}

public class UrgentOrderDto
{
    public int Id { get; set; }
    public string AssemblyUnit { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? PlannedFinishAt { get; set; }
}
