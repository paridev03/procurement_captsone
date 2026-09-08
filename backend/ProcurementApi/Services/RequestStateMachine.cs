using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Services;

public enum RequestAction
{
    Submit,
    Cancel,
    ManagerApprove,
    ManagerReject,
    FinanceApprove,
    FinanceReject,
    SelectVendor,
    TriggerPayment,
    RetryPayment,
    PaymentSucceeded,
    PaymentFailed
}

/// <summary>
/// Single source of truth for "what can happen next" (Section 3 of docs/ANALYSIS.md).
/// Nothing else in the codebase should branch on RequestStatus to decide whether a
/// transition is legal — everything routes through here so the rule can never drift
/// between controllers/services.
/// </summary>
public static class RequestStateMachine
{
    private static readonly Dictionary<(RequestStatus From, RequestAction Action), RequestStatus> Transitions = new()
    {
        [(RequestStatus.Draft, RequestAction.Submit)] = RequestStatus.Submitted,
        [(RequestStatus.Draft, RequestAction.Cancel)] = RequestStatus.Cancelled,
        [(RequestStatus.Submitted, RequestAction.Cancel)] = RequestStatus.Cancelled,
        [(RequestStatus.Submitted, RequestAction.ManagerApprove)] = RequestStatus.ManagerApproved,
        [(RequestStatus.Submitted, RequestAction.ManagerReject)] = RequestStatus.Rejected,
        [(RequestStatus.ManagerApproved, RequestAction.FinanceApprove)] = RequestStatus.FinanceApproved,
        [(RequestStatus.ManagerApproved, RequestAction.FinanceReject)] = RequestStatus.Rejected,
        [(RequestStatus.FinanceApproved, RequestAction.SelectVendor)] = RequestStatus.VendorSelected,
        [(RequestStatus.VendorSelected, RequestAction.TriggerPayment)] = RequestStatus.PaymentInProgress,
        [(RequestStatus.PaymentInProgress, RequestAction.PaymentSucceeded)] = RequestStatus.Completed,
        [(RequestStatus.PaymentInProgress, RequestAction.PaymentFailed)] = RequestStatus.PaymentFailed,
        [(RequestStatus.PaymentFailed, RequestAction.RetryPayment)] = RequestStatus.PaymentInProgress,
    };

    public static bool CanApply(RequestStatus from, RequestAction action) => Transitions.ContainsKey((from, action));

    public static RequestStatus Apply(RequestStatus from, RequestAction action)
    {
        if (!Transitions.TryGetValue((from, action), out var next))
        {
            throw new InvalidOperationException(
                $"Action '{action}' is not valid from status '{from}'.");
        }
        return next;
    }

    /// <summary>Which actions are legal right now — drives the "AvailableActions" the UI
    /// uses to decide which buttons to show, and lets 403 vs 409 be told apart cleanly.</summary>
    public static IEnumerable<RequestAction> AvailableActions(RequestStatus from) =>
        Transitions.Keys.Where(k => k.From == from).Select(k => k.Action);
}
