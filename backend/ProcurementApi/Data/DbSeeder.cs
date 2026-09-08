using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementApi.Domain.Entities;
using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Data;

/// <summary>Seeds one user per role plus the single vendor this phase needs, so the app is
/// usable immediately after `dotnet run` without a separate setup step.</summary>
public static class DbSeeder
{
    public const string DefaultPassword = "Passw0rd!";

    public static async Task SeedAsync(ProcurementDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync())
        {
            return;
        }

        var hasher = new PasswordHasher<User>();

        var manager = new User
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
        };
        procurement.PasswordHash = hasher.HashPassword(procurement, DefaultPassword);

        var vendor = new Vendor
        {
            Name = "Acme Office Supplies Ltd.",
            ContactEmail = "sales@acmeoffice.example",
            ContactPhone = "+1-555-0100",
            IsActive = true,
        };

        db.Users.AddRange(manager, employee, finance, procurement);
        db.Vendors.Add(vendor);

        await db.SaveChangesAsync();
    }
}
