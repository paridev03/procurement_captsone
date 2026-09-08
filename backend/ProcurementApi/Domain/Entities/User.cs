using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Department { get; set; } = string.Empty;

    /// <summary>Who approves this user's requests at the Manager stage. Null for non-employees.</summary>
    public Guid? ManagerId { get; set; }
    public User? Manager { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PurchaseRequest> Requests { get; set; } = new List<PurchaseRequest>();
}
