namespace DataVault.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<CategoryItem> CategoryItems { get; set; } = new List<CategoryItem>();
    public ICollection<WorkTask> WorkTasks { get; set; } = new List<WorkTask>();
}
