namespace TelecomProd.Core.Entities;

public class StockMovement
{
    public int Id { get; set; }
    public int ComponentId { get; set; }
    public int WarehouseId { get; set; }
    public string MovementType { get; set; } = string.Empty; // in, out, transfer
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Comment { get; set; }
    public Component Component { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
}
