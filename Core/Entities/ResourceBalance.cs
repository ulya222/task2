namespace DataVault.Core.Entities;

public class ResourceBalance
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public int StorageId { get; set; }
    public int Quantity { get; set; }
    public Resource Resource { get; set; } = null!;
    public Storage Storage { get; set; } = null!;
}
