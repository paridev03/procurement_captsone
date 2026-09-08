using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Domain.Entities;

public class PurchaseRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RequestNumber { get; set; } = string.Empty;

    public Guid RequesterId { get; set; }
    public User? Requester { get; set; }

    public string Title { get; set; } = string.Empty;
    public string BusinessJustification { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    public int EstimatedQuantity { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public decimal EstimatedTotalCost { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Draft;

    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public Payment? Payment { get; set; }

    /// <summary>Optimistic-concurrency token: guards against two approvers acting on the
    /// same request at the same time. Regenerated on every update (SQLite has no native
    /// rowversion column, so this is a manually-managed concurrency token instead).</summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ManagerApprovedAt { get; set; }
    public DateTime? FinanceApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<RequestApproval> Approvals { get; set; } = new List<RequestApproval>();
    public ICollection<RequestStatusHistory> History { get; set; } = new List<RequestStatusHistory>();
}
