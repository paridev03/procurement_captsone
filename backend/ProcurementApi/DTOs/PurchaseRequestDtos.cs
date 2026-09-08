using System.ComponentModel.DataAnnotations;

namespace ProcurementApi.DTOs;

public record CreatePurchaseRequestDto(
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(2000)] string BusinessJustification,
    [Required, MaxLength(100)] string Department,
    [Range(1, int.MaxValue)] int EstimatedQuantity,
    [Range(0.01, double.MaxValue)] decimal EstimatedUnitCost
);

public record UpdatePurchaseRequestDto(
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(2000)] string BusinessJustification,
    [Required, MaxLength(100)] string Department,
    [Range(1, int.MaxValue)] int EstimatedQuantity,
    [Range(0.01, double.MaxValue)] decimal EstimatedUnitCost
);

public record DecisionDto(string? Comment);

public record SelectVendorDto([Required] Guid VendorId);

public record PurchaseRequestSummaryDto(
    Guid Id,
    string RequestNumber,
    string Title,
    string Status,
    string RequesterName,
    string Department,
    decimal EstimatedTotalCost,
    DateTime CreatedAt
);

public record PurchaseRequestDetailDto(
    Guid Id,
    string RequestNumber,
    string Title,
    string BusinessJustification,
    string Department,
    int EstimatedQuantity,
    decimal EstimatedUnitCost,
    decimal EstimatedTotalCost,
    string Status,
    UserDto Requester,
    VendorDto? Vendor,
    PaymentDto? Payment,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ManagerApprovedAt,
    DateTime? FinanceApprovedAt,
    DateTime? CompletedAt,
    List<ApprovalDto> Approvals,
    List<HistoryDto> History,
    List<string> AvailableActions
);

public record ApprovalDto(string Stage, string Decision, string? Comment, string ApproverName, DateTime DecidedAt);

public record HistoryDto(string? FromStatus, string ToStatus, string ChangedByName, DateTime ChangedAt, string? Notes);

public record VendorDto(Guid Id, string Name, string ContactEmail, string ContactPhone);

public record PaymentDto(Guid Id, decimal Amount, string Method, string Status, string? TransactionReference, string? FailureReason, DateTime? ProcessedAt);
