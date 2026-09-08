using System.ComponentModel.DataAnnotations;

namespace ProcurementApi.DTOs;

public record CreatePurchaseRequestDto(
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(2000)] string BusinessJustification,
    [Required, MaxLength(100)] string Department,
    [Range(1, int.MaxValue)] int EstimatedQuantity,
    [Range(0.01, double.MaxValue)] decimal EstimatedUnitCost,
    [Required] string Category
);

public record UpdatePurchaseRequestDto(
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(2000)] string BusinessJustification,
    [Required, MaxLength(100)] string Department,
    [Range(1, int.MaxValue)] int EstimatedQuantity,
    [Range(0.01, double.MaxValue)] decimal EstimatedUnitCost,
    [Required] string Category
);

public record DecisionDto(string? Comment);

public record SelectVendorDto([Required] Guid VendorId);

// ---- Itemized procurement (Procurement Admin flow) ------------------------------

/// <summary>One line of an itemized request, as submitted by the client. `Id` is null for
/// a new line and set for an existing one being edited — that's how the service tells
/// "update this line" apart from "add a new line" without duplicating rows on save.
/// `UnitPrice`/`Description` are optional: omit them to take the catalog item's current
/// values, or send a value to override (e.g. a negotiated price) per business rules.</summary>
public record PurchaseRequestItemLineDto(
    Guid? Id,
    [Required] Guid ItemId,
    string? Description,
    [Range(1, int.MaxValue)] int Quantity,
    [Range(0.01, double.MaxValue)] decimal? UnitPrice
);

public record CreateItemizedPurchaseRequestDto(
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(2000)] string BusinessJustification,
    [Required, MaxLength(100)] string Department,
    [Required, MinLength(1)] List<PurchaseRequestItemLineDto> Items,
    [Required] string Category,
    [Range(0, 100)] decimal TaxRate = 0
);

public record UpdateItemizedPurchaseRequestDto(
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(2000)] string BusinessJustification,
    [Required, MaxLength(100)] string Department,
    [Required, MinLength(1)] List<PurchaseRequestItemLineDto> Items,
    [Required] string Category,
    [Range(0, 100)] decimal TaxRate = 0
);

public record ItemDto(Guid Id, string Code, string Name, string Description, decimal UnitPrice);

public record PurchaseRequestItemDto(
    Guid Id, Guid ItemId, string ItemCode, string Description, int Quantity, decimal UnitPrice, decimal TotalPrice
);

public record PurchaseRequestSummaryDto(
    Guid Id,
    string RequestNumber,
    string Title,
    string Status,
    string RequesterName,
    string Department,
    decimal EstimatedTotalCost,
    decimal TotalAmount,
    string Category,
    DateTime CreatedAt,
    DateTime? SubmittedAt
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
    List<string> AvailableActions,
    int TotalItems,
    int TotalQuantity,
    List<PurchaseRequestItemDto> Items,
    string? ModifiedByName,
    DateTime? ModifiedAt,
    decimal TaxAmount,
    decimal TotalAmount,
    bool HasInvoice,
    string Category,
    string? RecommendedVendorName
);

public record ApprovalDto(string Stage, string Decision, string? Comment, string ApproverName, DateTime DecidedAt);

public record HistoryDto(string? FromStatus, string ToStatus, string ChangedByName, DateTime ChangedAt, string? Notes);

public record VendorDto(Guid Id, string Name, string ContactEmail, string ContactPhone, string? TaxId);

public record PaymentDto(
    Guid Id, decimal Amount, string Method, string Status, string? TransactionReference, string? FailureReason, DateTime? ProcessedAt,
    string? PaymentReference, DateTime? PaymentDate, string? Notes
);

// ---- Payment Page --------------------------------------------------------------

/// <summary>Shared shape for both "Save as Draft" and "Mark as Paid" on the Payment Page —
/// the two actions differ only in how strictly the service validates the fields (draft
/// allows a partially-filled form; mark-paid requires Method and a positive Amount), not in
/// the DTO shape itself.</summary>
public record SavePaymentDto(
    string? PaymentReference,
    DateTime? PaymentDate,
    string? Method,
    decimal? Amount,
    string? TransactionReference,
    string? Notes
);

// ---- Invoice Page ---------------------------------------------------------------

public record GenerateInvoiceDto(
    [Range(0, double.MaxValue)] decimal DiscountAmount = 0,
    [Range(0, double.MaxValue)] decimal OtherCharges = 0
);

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    string ProcurementNumber,
    string ProcurementTitle,
    string Department,
    UserDto Requester,
    VendorDto? Vendor,
    List<PurchaseRequestItemDto> Items,
    decimal SubTotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal OtherCharges,
    decimal GrandTotal,
    string Status,
    string? PaymentReference,
    string? PaymentMethod,
    DateTime? PaymentDate,
    string PaymentStatus
);

// ---- Budget Service (Finance) ----------------------------------------------------

public record BudgetCheckDto(bool Available, decimal RemainingBudget);

// ---- Vendor quote (Procurement Admin) --------------------------------------------

public record VendorQuoteDto(string VendorName, bool Available, decimal QuotedUnitPrice, int EstimatedDeliveryDays);

// ---- Dashboard --------------------------------------------------------------------

public record DashboardSummaryDto(
    int Total, int Draft, int Pending, int Approved, int Rejected, int Completed, int PaymentFailed
);
