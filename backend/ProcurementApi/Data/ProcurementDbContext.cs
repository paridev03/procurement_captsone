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
    public DbSet<Item> Items => Set<Item>();
    public DbSet<PurchaseRequestItem> PurchaseRequestItems => Set<PurchaseRequestItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<DepartmentBudget> DepartmentBudgets => Set<DepartmentBudget>();
    public DbSet<Notification> Notifications => Set<Notification>();

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
            e.Property(r => r.TaxAmount).HasColumnType("decimal(18,2)");
            e.Property(r => r.TotalAmount).HasColumnType("decimal(18,2)");
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

            e.HasOne(r => r.Invoice)
                .WithOne(i => i.PurchaseRequest)
                .HasForeignKey<Invoice>(i => i.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.ModifiedByUser)
                .WithMany()
                .HasForeignKey(r => r.ModifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Item>(e =>
        {
            e.HasIndex(i => i.Code).IsUnique();
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<PurchaseRequestItem>(e =>
        {
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(i => i.TotalPrice).HasColumnType("decimal(18,2)");

            e.HasOne(i => i.PurchaseRequest)
                .WithMany(r => r.Items)
                .HasForeignKey(i => i.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.Item)
                .WithMany()
                .HasForeignKey(i => i.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
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

            e.HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.ModifiedByUser)
                .WithMany()
                .HasForeignKey(p => p.ModifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasIndex(i => i.InvoiceNumber).IsUnique();
            e.Property(i => i.SubTotal).HasColumnType("decimal(18,2)");
            e.Property(i => i.TaxAmount).HasColumnType("decimal(18,2)");
            e.Property(i => i.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(i => i.OtherCharges).HasColumnType("decimal(18,2)");
            e.Property(i => i.GrandTotal).HasColumnType("decimal(18,2)");

            e.HasOne(i => i.Payment)
                .WithMany()
                .HasForeignKey(i => i.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.CreatedByUser)
                .WithMany()
                .HasForeignKey(i => i.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.ModifiedByUser)
                .WithMany()
                .HasForeignKey(i => i.ModifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DepartmentBudget>(e =>
        {
            e.HasIndex(b => b.Department).IsUnique();
            e.Property(b => b.TotalBudget).HasColumnType("decimal(18,2)");
            e.Property(b => b.SpentAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasIndex(n => new { n.UserId, n.IsRead });

            e.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(n => n.PurchaseRequest)
                .WithMany()
                .HasForeignKey(n => n.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
