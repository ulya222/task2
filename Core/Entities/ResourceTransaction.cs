namespace DataVault.Core.Entities;

public class ResourceTransaction
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public int StorageId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Comment { get; set; }
    public Resource Resource { get; set; } = null!;
    public Storage Storage { get; set; } = null!;
}
