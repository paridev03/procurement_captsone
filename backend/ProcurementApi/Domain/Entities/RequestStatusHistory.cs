using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Domain.Entities;

/// <summary>Append-only audit trail. One row per state transition. This is the audit trail
/// the manual email-based process never had.</summary>
public class RequestStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PurchaseRequestId { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }

    public RequestStatus? FromStatus { get; set; }
    public RequestStatus ToStatus { get; set; }

    public Guid ChangedByUserId { get; set; }
    public User? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
