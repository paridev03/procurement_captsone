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

public enum ApprovalDecision
{
    Approved = 1,
    Rejected = 2
}

public enum PaymentMethod
{
    BankTransfer = 1
}

public enum PaymentStatus
{
    Pending = 1,
    Success = 2,
    Failed = 3
}
