namespace ProcurementApi.Services;

public record BudgetCheckResult(bool Available, decimal RemainingBudget);

/// <summary>Seam for the external Budget Service (Section 7 of the Phase 2 spec) —
/// abstracted the same way IPaymentGateway already is, so the simulated failure/latency
/// behaviour lives in one swappable implementation instead of scattered through Finance's
/// approval logic.</summary>
public interface IBudgetService
{
    Task<BudgetCheckResult> CheckAsync(string department, decimal amount, CancellationToken ct = default);
    Task SpendAsync(string department, decimal amount, CancellationToken ct = default);
}
