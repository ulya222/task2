namespace TelecomProd.Core.Entities;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactInfo { get; set; }
    public decimal ReliabilityRating { get; set; } // 0-100 или 0-5
    public int AvgDeliveryDays { get; set; }
    public ICollection<Component> Components { get; set; } = new List<Component>();
}
