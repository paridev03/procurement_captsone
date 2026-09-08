namespace ProcurementApi.Domain.Enums;

public enum UserRole
{
    Employee = 1,
    Manager = 2,
    Finance = 3,
    ProcurementAdmin = 4
}

/// <summary>
/// Every possible status a PurchaseRequest can be in.
/// The only place transitions between these are decided is RequestStateMachine.
/// </summary>
public enum RequestStatus
{
    Draft = 1,
    Submitted = 2,
    ManagerApproved = 3,
    FinanceApproved = 4,
    VendorSelected = 5,
    PaymentInProgress = 6,
    Completed = 7,
    Rejected = 8,
    PaymentFailed = 9,
    Cancelled = 10
}

public enum ApprovalStage
{
    ManagerApproval = 1,
    FinanceApproval = 2
}

/// <summary>Drives vendor recommendation (see VendorRecommendationRules) alongside the
/// existing free-text Department for budget lookups.</summary>
public enum Category
{
    IT_EQUIPMENT = 1,
    SOFTWARE = 2,
    OFFICE_SUPPLIES = 3,
    TRAVEL = 4,
    TRAINING = 5,
    OTHER = 6
}

public enum ApprovalDecision
{
    Approved = 1,
    Rejected = 2
}

public enum PaymentMethod
{
    BankTransfer = 1,
    UPI = 2,
    Cheque = 3,
    Cash = 4,
    Other = 5
}

/// <summary>Draft is new — a manually-entered payment being drafted on the Payment Page
/// before the user has confirmed it (see PurchaseRequestService.SavePaymentDraftAsync).
/// Success is displayed to the user as "Paid".</summary>
public enum PaymentStatus
{
    Pending = 1,
    Success = 2,
    Failed = 3,
    Draft = 4
}

/// <summary>Matches the Phase 2 spec's notification event vocabulary exactly (Section 13) —
/// one member per event a PurchaseRequest transition can raise. See NotificationObserver
/// for who gets notified for each.</summary>
public enum NotificationEvent
{
    RequestSubmitted = 1,
    ManagerApproved = 2,
    ManagerRejected = 3,
    FinanceApproved = 4,
    FinanceRejected = 5,
    VendorSelected = 6,
    PaymentStarted = 7,
    PaymentCompleted = 8,
    PaymentFailed = 9,
    RequestCompleted = 10
}

/// <summary>Only ever persisted as Generated — an invoice row existing at all means it has
/// been generated. "Pending" (no invoice yet) is represented by the absence of a row, not
/// a stored value; the enum exists for forward compatibility (e.g. a future Voided state).</summary>
public enum InvoiceStatus
{
    Generated = 1
}
