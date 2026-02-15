namespace DataVault.Core.Entities;

public class AppRole
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
