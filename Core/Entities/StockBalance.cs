namespace TelecomProd.Core.Entities;

public class StockBalance
{
    public int Id { get; set; }
    public int ComponentId { get; set; }
    public int WarehouseId { get; set; }
    public int Quantity { get; set; }
    public Component Component { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
}
