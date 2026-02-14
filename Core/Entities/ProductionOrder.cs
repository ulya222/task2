namespace TelecomProd.Core.Entities;

public class ProductionOrder
{
    public int Id { get; set; }
    public int AssemblyUnitId { get; set; }
    public int StatusId { get; set; }
    public int? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PlannedFinishAt { get; set; }
    public DateTime? ActualFinishAt { get; set; }
    public int Quantity { get; set; }
    public int AssemblyTimeMinutes { get; set; }
    public decimal UnitCost { get; set; }
    public AssemblyUnit AssemblyUnit { get; set; } = null!;
    public OrderStatus Status { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<QualityTest> QualityTests { get; set; } = new List<QualityTest>();
    public ICollection<DefectRecord> DefectRecords { get; set; } = new List<DefectRecord>();
}
