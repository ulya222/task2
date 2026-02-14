namespace TelecomProd.Core.Entities;

/// <summary>Строка спецификации (Bill of Materials).</summary>
public class BomItem
{
    public int Id { get; set; }
    public int AssemblyUnitId { get; set; }
    public int ComponentId { get; set; }
    public int Quantity { get; set; }
    public AssemblyUnit AssemblyUnit { get; set; } = null!;
    public Component Component { get; set; } = null!;
}
