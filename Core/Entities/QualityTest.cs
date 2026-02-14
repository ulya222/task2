namespace TelecomProd.Core.Entities;

public class QualityTest
{
    public int Id { get; set; }
    public int ProductionOrderId { get; set; }
    public string TestProcedure { get; set; } = string.Empty;
    public string? MeasurementResult { get; set; }
    public bool Passed { get; set; }
    public DateTime TestedAt { get; set; }
    public string? CertificateNumber { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;
}
