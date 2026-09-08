using Microsoft.EntityFrameworkCore;
using ProcurementApi.Data;
using ProcurementApi.Domain.Entities;

namespace ProcurementApi.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly ProcurementDbContext _db;
    public ItemRepository(ProcurementDbContext db) => _db = db;

    public Task<List<Item>> GetActiveAsync(CancellationToken ct = default) =>
        _db.Items.Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync(ct);

    public Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Items.FirstOrDefaultAsync(i => i.Id == id, ct);
}
