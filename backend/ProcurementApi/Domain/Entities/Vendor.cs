namespace ProcurementApi.Domain.Entities;

public class Vendor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public bool IsActive { get; set; } = true;
}
