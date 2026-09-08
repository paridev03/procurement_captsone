namespace ProcurementApi.Domain.Entities;

/// <summary>Item/material catalog — master data selectable on an itemized purchase request,
/// the same role <see cref="Vendor"/> plays for vendor selection.</summary>
public class Item
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
}
