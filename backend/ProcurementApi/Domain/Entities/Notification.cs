using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Domain.Entities;

/// <summary>One in-app notification for one recipient. Created by NotificationObserver
/// reacting to a PurchaseRequest state change (see IRequestEventObserver) — this entity
/// itself carries no business logic, just the delivered record.</summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public NotificationEvent Event { get; set; }
    public string Message { get; set; } = string.Empty;

    public Guid? PurchaseRequestId { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
