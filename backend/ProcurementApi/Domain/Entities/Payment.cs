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
}
