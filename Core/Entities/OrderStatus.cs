namespace TelecomProd.Core.Entities;

public class OrderStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();
}
