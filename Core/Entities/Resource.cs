using System.ComponentModel.DataAnnotations.Schema;

namespace DataVault.Core.Entities;

public class Resource
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ResourceKind { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string? SpecsJson { get; set; }
    public string UnitOfMeasure { get; set; } = "шт";
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? ImageUrl { get; set; }
    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public ICollection<ResourceBalance> ResourceBalances { get; set; } = new List<ResourceBalance>();
    public ICollection<CategoryItem> CategoryItems { get; set; } = new List<CategoryItem>();
    public ICollection<ResourceTransaction> ResourceTransactions { get; set; } = new List<ResourceTransaction>();

    [NotMapped]
    public int TotalStock => ResourceBalances?.Sum(rb => rb.Quantity) ?? 0;
}
