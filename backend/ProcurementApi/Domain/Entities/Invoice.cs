using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Domain.Entities;

/// <summary>The final stage of the Procurement -> Payment -> Invoice workflow. 1:1 with the
/// PurchaseRequest it bills and the Payment that funded it. SubTotal/TaxAmount are snapshot
/// from the procurement at generation time (the procurement is already read-only by then,
/// having passed through Submitted, so this is never out of sync) — DiscountAmount/
/// OtherCharges/GrandTotal are specific to the invoice itself.</summary>
public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PurchaseRequestId { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }

    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal OtherCharges { get; set; }
    public decimal GrandTotal { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Generated;

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedByUserId { get; set; }
    public User? ModifiedByUser { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
