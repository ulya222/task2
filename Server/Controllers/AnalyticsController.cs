using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataVault.Core;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DataVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly DataVaultDbContext _context;
    public AnalyticsController(DataVaultDbContext context) => _context = context;

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var today = DateTime.UtcNow.Date;
        var tasksToday = await _context.WorkTasks.CountAsync(t => t.CreatedAt.Date == today);
        var tasksInProgress = await _context.WorkTasks.CountAsync(t => t.PhaseId >= 2 && t.PhaseId <= 5);
        var lowStock = await _context.ResourceBalances
            .Include(rb => rb.Resource)
            .Where(rb => rb.Quantity <= rb.Resource.MinStock && rb.Quantity >= 0)
            .Select(rb => new LowStockDto { Code = rb.Resource!.Code, Name = rb.Resource.Name, Quantity = rb.Quantity, MinStock = rb.Resource.MinStock })
            .Take(20).ToListAsync();
        var urgentTasks = await _context.WorkTasks
            .Include(t => t.Category).Include(t => t.Phase)
            .Where(t => t.PhaseId < 6 && t.PlannedFinishAt.HasValue && t.PlannedFinishAt <= today.AddDays(2))
            .OrderBy(t => t.PlannedFinishAt).Take(10)
            .Select(t => new UrgentTaskDto { Id = t.Id, CategoryName = t.Category!.Name, PhaseName = t.Phase!.Name, PlannedFinishAt = t.PlannedFinishAt })
            .ToListAsync();
        return Ok(new OverviewDto { TasksToday = tasksToday, TasksInProgress = tasksInProgress, LowStock = lowStock, UrgentTasks = urgentTasks });
    }

    [HttpGet("export/resources")]
    public async Task<IActionResult> ExportResources([FromQuery] string? search)
    {
        var query = _context.Resources.Include(r => r.Vendor).Include(r => r.ResourceBalances).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Name.Contains(search) || r.Code.Contains(search));
        var list = await query.OrderBy(r => r.Code).ToListAsync();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Ресурсы");
        ws.Cell(1, 1).Value = "Код"; ws.Cell(1, 2).Value = "Наименование"; ws.Cell(1, 3).Value = "Тип";
        ws.Cell(1, 4).Value = "Производитель"; ws.Cell(1, 5).Value = "Остаток"; ws.Cell(1, 6).Value = "Мин"; ws.Cell(1, 7).Value = "Макс";
        int row = 2;
        foreach (var r in list)
        {
            var total = r.ResourceBalances?.Sum(rb => rb.Quantity) ?? 0;
            ws.Cell(row, 1).Value = r.Code; ws.Cell(row, 2).Value = r.Name; ws.Cell(row, 3).Value = r.ResourceKind;
            ws.Cell(row, 4).Value = r.Manufacturer; ws.Cell(row, 5).Value = total; ws.Cell(row, 6).Value = r.MinStock; ws.Cell(row, 7).Value = r.MaxStock;
            row++;
        }
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "resources.xlsx");
    }

    [HttpGet("export/tasks")]
    public async Task<IActionResult> ExportTasks([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDt = from ?? DateTime.UtcNow.AddDays(-30);
        var toDt = to ?? DateTime.UtcNow;
        var list = await _context.WorkTasks
            .Include(t => t.Category).Include(t => t.Phase).Include(t => t.User)
            .Where(t => t.CreatedAt >= fromDt && t.CreatedAt <= toDt)
            .OrderByDescending(t => t.CreatedAt).ToListAsync();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Задачи");
        ws.Cell(1, 1).Value = "№"; ws.Cell(1, 2).Value = "Категория"; ws.Cell(1, 3).Value = "Фаза";
        ws.Cell(1, 4).Value = "Кол-во"; ws.Cell(1, 5).Value = "Дата"; ws.Cell(1, 6).Value = "Себестоимость";
        int row = 2;
        foreach (var t in list)
        {
            ws.Cell(row, 1).Value = t.Id; ws.Cell(row, 2).Value = t.Category?.Name; ws.Cell(row, 3).Value = t.Phase?.Name;
            ws.Cell(row, 4).Value = t.Quantity; ws.Cell(row, 5).Value = t.CreatedAt; ws.Cell(row, 6).Value = t.UnitCost;
            row++;
        }
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "tasks.xlsx");
    }

    [HttpGet("export/overview-pdf")]
    public async Task<IActionResult> ExportOverviewPdf()
    {
        var today = DateTime.UtcNow.Date;
        var tasksToday = await _context.WorkTasks.CountAsync(t => t.CreatedAt.Date == today);
        var tasksInProgress = await _context.WorkTasks.CountAsync(t => t.PhaseId >= 2 && t.PhaseId <= 5);
        var lowCount = await _context.ResourceBalances.Include(rb => rb.Resource)
            .CountAsync(rb => rb.Quantity <= rb.Resource.MinStock);
        QuestPDF.Settings.License = LicenseType.Community;
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.Header().Text("Сводный отчёт DataVault").Bold().FontSize(18);
                page.Content().Column(col =>
                {
                    col.Item().Text($"Дата: {DateTime.UtcNow:dd.MM.yyyy HH:mm}").FontSize(10);
                    col.Item().PaddingVertical(10).Text($"Задач за сегодня: {tasksToday}").FontSize(12);
                    col.Item().Text($"В работе: {tasksInProgress}").FontSize(12);
                    col.Item().Text($"Позиций с низким остатком: {lowCount}").FontSize(12);
                });
            });
        });
        var stream = new MemoryStream();
        doc.GeneratePdf(stream);
        stream.Position = 0;
        return File(stream.ToArray(), "application/pdf", "overview.pdf");
    }
}

public class OverviewDto
{
    public int TasksToday { get; set; }
    public int TasksInProgress { get; set; }
    public List<LowStockDto> LowStock { get; set; } = new();
    public List<UrgentTaskDto> UrgentTasks { get; set; } = new();
}

public class LowStockDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public int MinStock { get; set; }
}

public class UrgentTaskDto
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = "";
    public string PhaseName { get; set; } = "";
    public DateTime? PlannedFinishAt { get; set; }
}
