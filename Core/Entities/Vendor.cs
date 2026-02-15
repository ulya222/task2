namespace DataVault.Core.Entities;

public class Vendor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactInfo { get; set; }
    public decimal ReliabilityRating { get; set; }
    public int AvgDeliveryDays { get; set; }
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
}
