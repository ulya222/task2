namespace DataVault.Core.Entities;

public class WorkTask
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public int PhaseId { get; set; }
    public int? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PlannedFinishAt { get; set; }
    public DateTime? ActualFinishAt { get; set; }
    public int Quantity { get; set; }
    public int EstimatedMinutes { get; set; }
    public decimal UnitCost { get; set; }
    public Category Category { get; set; } = null!;
    public TaskPhase Phase { get; set; } = null!;
    public AppUser? User { get; set; }
    public ICollection<Verification> Verifications { get; set; } = new List<Verification>();
    public ICollection<Remark> Remarks { get; set; } = new List<Remark>();
}
