using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PurchaseRequestId { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? TransactionReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // ---- manual Payment Page fields (Procurement -> Payment -> Invoice workflow) -------
    // Distinct from TransactionReference (the automatic gateway's own reference) — this is
    // a human-entered reference for the payment record itself (e.g. a cheque/bank ref).
    public string? PaymentReference { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Notes { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedByUserId { get; set; }
    public User? ModifiedByUser { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
