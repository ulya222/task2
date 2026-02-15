namespace DataVault.Core.Entities;

public class TaskPhase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<WorkTask> WorkTasks { get; set; } = new List<WorkTask>();
}
