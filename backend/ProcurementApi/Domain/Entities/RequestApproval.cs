using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Domain.Entities;

/// <summary>One row per Manager/Finance decision made on a request.</summary>
public class RequestApproval
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PurchaseRequestId { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }

    public Guid ApproverId { get; set; }
    public User? Approver { get; set; }

    public ApprovalStage Stage { get; set; }
    public ApprovalDecision Decision { get; set; }
    public string? Comment { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}
