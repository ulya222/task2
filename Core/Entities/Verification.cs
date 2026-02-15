namespace DataVault.Core.Entities;

public class Verification
{
    public int Id { get; set; }
    public int WorkTaskId { get; set; }
    public string ProcedureName { get; set; } = string.Empty;
    public string? ResultValue { get; set; }
    public bool Passed { get; set; }
    public DateTime VerifiedAt { get; set; }
    public string? CertificateNumber { get; set; }
    public WorkTask WorkTask { get; set; } = null!;
}
