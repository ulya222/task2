namespace DataVault.Core.Entities;

public class Storage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StorageKind { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? TempRegime { get; set; }
    public ICollection<ResourceBalance> ResourceBalances { get; set; } = new List<ResourceBalance>();
    public ICollection<ResourceTransaction> ResourceTransactions { get; set; } = new List<ResourceTransaction>();
}
