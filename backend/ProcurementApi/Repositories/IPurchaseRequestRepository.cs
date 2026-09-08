using ProcurementApi.Domain.Entities;

namespace ProcurementApi.Repositories;

public interface IPurchaseRequestRepository
{
    Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<PurchaseRequest>> GetAllAsync(CancellationToken ct = default);
    Task<List<PurchaseRequest>> GetByRequesterAsync(Guid requesterId, CancellationToken ct = default);
    Task<List<PurchaseRequest>> GetPendingManagerApprovalAsync(Guid managerId, CancellationToken ct = default);
    Task<List<PurchaseRequest>> GetByStatusAsync(Domain.Enums.RequestStatus status, CancellationToken ct = default);
    Task AddAsync(PurchaseRequest request, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<int> GetNextSequenceForYearAsync(int year, CancellationToken ct = default);
}
