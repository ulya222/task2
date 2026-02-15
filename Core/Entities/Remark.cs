namespace DataVault.Core.Entities;

public class Remark
{
    public int Id { get; set; }
    public int WorkTaskId { get; set; }
    public string RemarkType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime RecordedAt { get; set; }
    public WorkTask WorkTask { get; set; } = null!;
}
