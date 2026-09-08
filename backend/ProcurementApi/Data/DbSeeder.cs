using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementApi.Domain.Entities;
using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Data;

/// <summary>Seeds one user per role, the single vendor, and the item catalog this phase
/// needs, so the app is usable immediately after `dotnet run` without a separate setup
/// step. Each table is seeded independently (rather than one all-or-nothing guard) so a
/// database that already has users from an earlier run still picks up newly-added seed
/// data — e.g. the item catalog added for the itemized Procurement Admin flow — the next
/// time the app starts.</summary>
public static class DbSeeder
{
    public const string DefaultPassword = "Passw0rd!";

    public static async Task SeedAsync(ProcurementDbContext db)
    {
        await db.Database.MigrateAsync();

        var hasher = new PasswordHasher<User>();
        User? manager = null;

        if (!await db.Users.AnyAsync())
        {
            manager = new User
            {
                FullName = "Morgan Lee",
                Email = "manager@procurement.local",
                Role = UserRole.Manager,
                Department = "Engineering",
            };
            manager.PasswordHash = hasher.HashPassword(manager, DefaultPassword);

            var employee = new User
            {
                FullName = "Alex Chen",
                Email = "employee@procurement.local",
                Role = UserRole.Employee,
                Department = "Engineering",
                ManagerId = manager.Id,
            };
            employee.PasswordHash = hasher.HashPassword(employee, DefaultPassword);

            var finance = new User
            {
                FullName = "Priya Nair",
                Email = "finance@procurement.local",
                Role = UserRole.Finance,
                Department = "Finance",
            };
            finance.PasswordHash = hasher.HashPassword(finance, DefaultPassword);

            var procurement = new User
            {
                FullName = "Sam Torres",
                Email = "procurement@procurement.local",
                Role = UserRole.ProcurementAdmin,
                Department = "Procurement",
                // Requests the Procurement Admin itself raises (the itemized flow) still
                // need to clear Manager approval like any other request — without this, a
                // request they submit could never be approved (no manager owns it) and
                // would be stuck at Submitted forever.
                ManagerId = manager.Id,
            };
            procurement.PasswordHash = hasher.HashPassword(procurement, DefaultPassword);

            db.Users.AddRange(manager, employee, finance, procurement);
        }

        // Per-vendor existence checks (not one all-or-nothing guard) so a database that
        // already had Acme seeded before these three quote-integrated vendors were added
        // still picks them up on next startup — same reasoning as the Items/ManagerId
        // self-heals below.
        var existingVendorNames = await db.Vendors.Select(v => v.Name).ToListAsync();
        var candidateVendors = new[]
        {
            new Vendor { Name = "Acme Office Supplies Ltd.", ContactEmail = "sales@acmeoffice.example", ContactPhone = "+1-555-0100", TaxId = "GSTIN-29ACME1234F1Z5", IsActive = true },
            // These three have a live "Request Vendor Quote" integration — see
            // Services/Vendors/*Gateway.cs and VendorRecommendationRules.
            new Vendor { Name = "TechSource", ContactEmail = "orders@techsource.example", ContactPhone = "+1-555-0201", TaxId = "GSTIN-27TECH5678K1Z2", IsActive = true },
            new Vendor { Name = "OfficeMart", ContactEmail = "orders@officemart.example", ContactPhone = "+1-555-0202", TaxId = "GSTIN-27OMRT9012L1Z3", IsActive = true },
            new Vendor { Name = "EnterpriseSupply", ContactEmail = "orders@enterprisesupply.example", ContactPhone = "+1-555-0203", TaxId = "GSTIN-27ENTS3456M1Z4", IsActive = true },
        };
        db.Vendors.AddRange(candidateVendors.Where(v => !existingVendorNames.Contains(v.Name)));

        // Same per-row self-heal for department budgets, backing the simulated Budget
        // Service (see SimulatedBudgetService) — a running balance Finance's approval
        // spends against.
        var existingBudgetDepartments = await db.DepartmentBudgets.Select(b => b.Department).ToListAsync();
        var candidateBudgets = new[]
        {
            new DepartmentBudget { Department = "Engineering", TotalBudget = 500_000m, SpentAmount = 0 },
            new DepartmentBudget { Department = "Finance", TotalBudget = 200_000m, SpentAmount = 0 },
            new DepartmentBudget { Department = "Procurement", TotalBudget = 300_000m, SpentAmount = 0 },
            new DepartmentBudget { Department = "Marketing", TotalBudget = 150_000m, SpentAmount = 0 },
            new DepartmentBudget { Department = "Sales", TotalBudget = 150_000m, SpentAmount = 0 },
        };
        db.DepartmentBudgets.AddRange(candidateBudgets.Where(b => !existingBudgetDepartments.Contains(b.Department)));

        if (!await db.Items.AnyAsync())
        {
            db.Items.AddRange(
                new Item { Code = "ITM-001", Name = "Laptop", Description = "Business laptop, 14-inch, 16GB RAM", UnitPrice = 1200.00m, IsActive = true },
                new Item { Code = "ITM-002", Name = "Monitor", Description = "27-inch LED monitor, 1440p", UnitPrice = 250.00m, IsActive = true },
                new Item { Code = "ITM-003", Name = "Office Chair", Description = "Ergonomic mesh-back office chair", UnitPrice = 180.00m, IsActive = true },
                new Item { Code = "ITM-004", Name = "Standing Desk", Description = "Electric height-adjustable desk", UnitPrice = 450.00m, IsActive = true },
                new Item { Code = "ITM-005", Name = "Wireless Keyboard", Description = "Bluetooth mechanical keyboard", UnitPrice = 75.00m, IsActive = true },
                new Item { Code = "ITM-006", Name = "Wireless Mouse", Description = "Ergonomic wireless mouse", UnitPrice = 35.00m, IsActive = true },
                new Item { Code = "ITM-007", Name = "Printer Paper (Case)", Description = "A4 80gsm, 5 reams per case", UnitPrice = 42.00m, IsActive = true },
                new Item { Code = "ITM-008", Name = "Toner Cartridge", Description = "Black toner cartridge, standard yield", UnitPrice = 95.00m, IsActive = true }
            );
        }

        // Self-heal a database seeded before the ManagerId fix above existed (same reasoning:
        // a Procurement Admin's own submitted request needs a manager to approve it).
        var procurementAdmin = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.ProcurementAdmin && u.ManagerId == null);
        if (procurementAdmin is not null)
        {
            var anyManager = manager ?? await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Manager);
            if (anyManager is not null)
            {
                procurementAdmin.ManagerId = anyManager.Id;
            }
        }

        await db.SaveChangesAsync();
    }
}
