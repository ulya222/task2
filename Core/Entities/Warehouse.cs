namespace TelecomProd.Core.Entities;

public class Warehouse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WarehouseType { get; set; } = string.Empty; // main, production, reject
    public int Capacity { get; set; }
    public string? TempRegime { get; set; }
    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
