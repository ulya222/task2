namespace TelecomProd.Core.Entities;

public class DefectRecord
{
    public int Id { get; set; }
    public int ProductionOrderId { get; set; }
    public string DefectType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime RecordedAt { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;
}
