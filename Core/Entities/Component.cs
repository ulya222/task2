using System.ComponentModel.DataAnnotations.Schema;

namespace TelecomProd.Core.Entities;

/// <summary>Компонент (материал) для производства узлов связи. Код формата TYPE-XXXXX.</summary>
public class Component
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty; // TYPE-XXXXX
    public string Name { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty; // active/passive, electronic/mechanical
    public string Manufacturer { get; set; } = string.Empty;
    public string? TechSpecsJson { get; set; } // JSON технических характеристик
    public string UnitOfMeasure { get; set; } = "шт";
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? ImageUrl { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
    public ICollection<BomItem> BomItems { get; set; } = new List<BomItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    [NotMapped]
    public int TotalStock => StockBalances?.Sum(sb => sb.Quantity) ?? 0;
}
