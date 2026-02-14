namespace TelecomProd.Core.Entities;

/// <summary>Тип узла связи (изделие) — BOM-шаблон.</summary>
public class AssemblyUnit
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<BomItem> BomItems { get; set; } = new List<BomItem>();
    public ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();
}
