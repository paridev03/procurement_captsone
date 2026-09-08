using Microsoft.EntityFrameworkCore;
using ProcurementApi.Domain.Entities;

namespace ProcurementApi.Data;

public class ProcurementDbContext : DbContext
{
    public ProcurementDbContext(DbContextOptions<ProcurementDbContext> options) : base(options) { }

    /// <summary>Bumps the concurrency stamp on every modified PurchaseRequest so the
    /// manual optimistic-concurrency check (see ConcurrencyStamp) actually fires.</summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        BumpConcurrencyStamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        BumpConcurrencyStamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void BumpConcurrencyStamps()
    {
        foreach (var entry in ChangeTracker.Entries<PurchaseRequest>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ConcurrencyStamp = Guid.NewGuid();
            }
        }
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<RequestApproval> RequestApprovals => Set<RequestApproval>();
    public DbSet<RequestStatusHistory> RequestStatusHistories => Set<RequestStatusHistory>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Manager)
                .WithMany()
                .HasForeignKey(u => u.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseRequest>(e =>
        {
            e.HasIndex(r => r.RequestNumber).IsUnique();
            e.Property(r => r.EstimatedUnitCost).HasColumnType("decimal(18,2)");
            e.Property(r => r.EstimatedTotalCost).HasColumnType("decimal(18,2)");
            e.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();

            e.HasOne(r => r.Requester)
                .WithMany(u => u.Requests)
                .HasForeignKey(r => r.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Vendor)
                .WithMany()
                .HasForeignKey(r => r.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Payment)
                .WithOne(p => p.PurchaseRequest)
                .HasForeignKey<Payment>(p => p.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RequestApproval>(e =>
        {
            e.HasOne(a => a.PurchaseRequest)
                .WithMany(r => r.Approvals)
                .HasForeignKey(a => a.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Approver)
                .WithMany()
                .HasForeignKey(a => a.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RequestStatusHistory>(e =>
        {
            e.HasOne(h => h.PurchaseRequest)
                .WithMany(r => r.History)
                .HasForeignKey(h => h.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(h => h.ChangedBy)
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        });
    }
}
