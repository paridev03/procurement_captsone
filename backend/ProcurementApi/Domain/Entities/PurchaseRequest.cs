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

    /// <summary>Drives vendor recommendation (see VendorRecommendationRules) and the
    /// Budget Service's department/category-scoped check.</summary>
    public Category Category { get; set; } = Category.OTHER;

    public int EstimatedQuantity { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public decimal EstimatedTotalCost { get; set; }

    /// <summary>Header-level rollups for the itemized Procurement Admin flow (see
    /// PurchaseRequestService.RecalculateItemizedTotals) — always recomputed server-side
    /// from <see cref="Items"/>, never trusted from the client. Zero/unused for the plain
    /// single-line Employee request flow, which has no child items.</summary>
    public int TotalItems { get; set; }
    public int TotalQuantity { get; set; }

    /// <summary>EstimatedTotalCost above is the pre-tax "SubTotal" (sum of line totals for
    /// the itemized flow). TaxAmount is computed server-side from the client-supplied tax
    /// rate at save time; TotalAmount = EstimatedTotalCost + TaxAmount is what Manager/
    /// Finance approve and what flows into Payment/Invoice. Zero for the plain Employee
    /// flow (no tax concept there), where TotalAmount always equals EstimatedTotalCost.</summary>
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Draft;

    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public Payment? Payment { get; set; }
    public Invoice? Invoice { get; set; }

    public ICollection<PurchaseRequestItem> Items { get; set; } = new List<PurchaseRequestItem>();

    public Guid? ModifiedByUserId { get; set; }
    public User? ModifiedByUser { get; set; }
    public DateTime? ModifiedAt { get; set; }

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
