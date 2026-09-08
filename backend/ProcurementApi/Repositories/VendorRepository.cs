using Microsoft.EntityFrameworkCore;
using ProcurementApi.Data;
using ProcurementApi.Domain.Entities;

namespace ProcurementApi.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly ProcurementDbContext _db;
    public VendorRepository(ProcurementDbContext db) => _db = db;

    public Task<List<Vendor>> GetActiveAsync(CancellationToken ct = default) =>
        _db.Vendors.Where(v => v.IsActive).OrderBy(v => v.Name).ToListAsync(ct);

    public Task<Vendor?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Vendors.FirstOrDefaultAsync(v => v.Id == id, ct);
}
