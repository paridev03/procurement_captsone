using ProcurementApi.Domain.Entities;

namespace ProcurementApi.Repositories;

public interface IItemRepository
{
    Task<List<Item>> GetActiveAsync(CancellationToken ct = default);
    Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
