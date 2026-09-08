using ProcurementApi.Domain.Entities;

namespace ProcurementApi.Repositories;

public interface IVendorRepository
{
    Task<List<Vendor>> GetActiveAsync(CancellationToken ct = default);
    Task<Vendor?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
