using Microsoft.EntityFrameworkCore;
using ProcurementApi.Data;
using ProcurementApi.Domain.Entities;
using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Repositories;

public class PurchaseRequestRepository : IPurchaseRequestRepository
{
    private readonly ProcurementDbContext _db;

    public PurchaseRequestRepository(ProcurementDbContext db)
    {
        _db = db;
    }

    // AsSplitQuery: PurchaseRequest has two independent collections (Approvals, History).
    // A single query would join both together (cartesian explosion) and, worse, was
    // observed to confuse SaveChanges into emitting an UPDATE for a brand-new History row
    // instead of an INSERT. Splitting into one query per collection avoids both problems.
    private IQueryable<PurchaseRequest> WithGraph() =>
        _db.PurchaseRequests
            .Include(r => r.Requester)
            .Include(r => r.Vendor)
            .Include(r => r.Payment)
            .Include(r => r.Approvals).ThenInclude(a => a.Approver)
            .Include(r => r.History).ThenInclude(h => h.ChangedBy)
            .AsSplitQuery();

    public Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        WithGraph().FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<List<PurchaseRequest>> GetAllAsync(CancellationToken ct = default) =>
        WithGraph().OrderByDescending(r => r.CreatedAt).ToListAsync(ct);

    public Task<List<PurchaseRequest>> GetByRequesterAsync(Guid requesterId, CancellationToken ct = default) =>
        WithGraph().Where(r => r.RequesterId == requesterId).OrderByDescending(r => r.CreatedAt).ToListAsync(ct);

    public Task<List<PurchaseRequest>> GetPendingManagerApprovalAsync(Guid managerId, CancellationToken ct = default) =>
        WithGraph()
            .Where(r => r.Status == RequestStatus.Submitted && r.Requester!.ManagerId == managerId)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync(ct);

    public Task<List<PurchaseRequest>> GetByStatusAsync(RequestStatus status, CancellationToken ct = default) =>
        WithGraph().Where(r => r.Status == status).OrderBy(r => r.CreatedAt).ToListAsync(ct);

    public Task AddAsync(PurchaseRequest request, CancellationToken ct = default)
    {
        _db.PurchaseRequests.Add(request);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task<int> GetNextSequenceForYearAsync(int year, CancellationToken ct = default)
    {
        var prefix = $"PR-{year}-";
        var count = await _db.PurchaseRequests.CountAsync(r => r.RequestNumber.StartsWith(prefix), ct);
        return count + 1;
    }
}
