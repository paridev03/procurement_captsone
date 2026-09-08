namespace ProcurementApi.Domain.Entities;

/// <summary>One line of an itemized purchase request (the "Procurement Item" of the
/// itemized Procurement Admin flow — see PurchaseRequestService.CreateItemizedDraftAsync).
/// Code/Description/UnitPrice are snapshotted from the <see cref="Item"/> catalog at the
/// time the line is added/edited, so a later catalog price change never retroactively
/// changes an existing request — the same reasoning RequestApproval/RequestStatusHistory
/// already snapshot the actor's name rather than only keeping a FK.</summary>
public class PurchaseRequestItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PurchaseRequestId { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }

    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Always Quantity * UnitPrice, recomputed server-side on every save —
    /// never trusted from the client.</summary>
    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}
