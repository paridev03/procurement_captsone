using Microsoft.EntityFrameworkCore;
using ProcurementApi.Data;

namespace ProcurementApi.Services;

/// <summary>Stands in for the external Budget Service (Phase 2 spec, Section 7) — there is
/// no real department-budget system to call, so this simulates one: a real running balance
/// per department backed by DepartmentBudget, plus the spec's documented failure profile
/// (70% normal, 20% simulated HTTP 500, 10% slow response) so the app can demonstrate
/// timeout/retry behaviour the same way a real flaky dependency would require.</summary>
public class SimulatedBudgetService : IBudgetService
{
    private readonly ProcurementDbContext _db;
    private static readonly Random _random = new();

    // Departments an Employee/Procurement Admin might type in freely have no seeded row —
    // rather than block the demo on an unrecognized department, fall back to a generous
    // default so the check still behaves sensibly.
    private const decimal DefaultBudgetForUnknownDepartment = 500_000m;

    public SimulatedBudgetService(ProcurementDbContext db)
    {
        _db = db;
    }

    public async Task<BudgetCheckResult> CheckAsync(string department, decimal amount, CancellationToken ct = default)
    {
        await SimulateExternalCallAsync(ct);

        var budget = await _db.DepartmentBudgets.FirstOrDefaultAsync(
            b => b.Department.ToLower() == department.ToLower(), ct);

        var remaining = budget is null ? DefaultBudgetForUnknownDepartment : budget.TotalBudget - budget.SpentAmount;
        return new BudgetCheckResult(amount <= remaining, remaining);
    }

    public async Task SpendAsync(string department, decimal amount, CancellationToken ct = default)
    {
        var budget = await _db.DepartmentBudgets.FirstOrDefaultAsync(
            b => b.Department.ToLower() == department.ToLower(), ct);

        // No seeded budget row for this department — nothing to decrement (see the
        // generous-default fallback in CheckAsync above; same reasoning applies here).
        if (budget is null) return;

        budget.SpentAmount += amount;
        await _db.SaveChangesAsync(ct);
    }

    private static async Task SimulateExternalCallAsync(CancellationToken ct)
    {
        var roll = _random.NextDouble();
        if (roll < 0.20)
        {
            throw new ExternalServiceException("Budget Service returned an error (simulated failure).");
        }
        if (roll < 0.30)
        {
            // Slow response, not a failure — the caller's retry wrapper only reacts to
            // ExternalServiceException, so this path always eventually succeeds.
            await Task.Delay(2000, ct);
        }
    }
}
