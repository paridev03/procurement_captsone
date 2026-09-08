namespace ProcurementApi.Domain.Entities;

/// <summary>Backs the simulated external Budget Service (see IBudgetService) — a running
/// balance per department that Finance checks/spends against when approving a request.</summary>
public class DepartmentBudget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Department { get; set; } = string.Empty;
    public decimal TotalBudget { get; set; }
    public decimal SpentAmount { get; set; }
}
