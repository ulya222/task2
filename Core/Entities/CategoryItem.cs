namespace DataVault.Core.Entities;

public class CategoryItem
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public int ResourceId { get; set; }
    public int Quantity { get; set; }
    public Category Category { get; set; } = null!;
    public Resource Resource { get; set; } = null!;
}
