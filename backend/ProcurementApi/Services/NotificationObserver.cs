using Microsoft.EntityFrameworkCore;
using ProcurementApi.Data;
using ProcurementApi.Domain.Entities;
using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Services;

/// <summary>
/// The one place that decides, for a given PurchaseRequest event, who should be notified
/// and what the message says (Section 13 of the Phase 2 spec). Deliberately separate from
/// NotificationService (which only knows how to store/query a notification) and from
/// PurchaseRequestService (which only knows the business rules for transitions) — each of
/// the three has exactly one reason to change (SRP).
/// </summary>
public class NotificationObserver : IRequestEventObserver
{
    private readonly INotificationService _notifications;
    private readonly ProcurementDbContext _db;

    public NotificationObserver(INotificationService notifications, ProcurementDbContext db)
    {
        _notifications = notifications;
        _db = db;
    }

    public async Task OnRequestEventAsync(PurchaseRequest request, NotificationEvent evt, string note, CancellationToken ct = default)
    {
        var recipients = await ResolveRecipientsAsync(request, evt, ct);
        var message = BuildMessage(request, evt);
        foreach (var recipientId in recipients)
        {
            await _notifications.CreateAsync(recipientId, evt, message, request.Id, ct);
        }
    }

    private async Task<List<Guid>> ResolveRecipientsAsync(PurchaseRequest request, NotificationEvent evt, CancellationToken ct)
    {
        switch (evt)
        {
            case NotificationEvent.RequestSubmitted:
            {
                // The requester's manager is who needs to act next.
                var managerId = await _db.Users
                    .Where(u => u.Id == request.RequesterId)
                    .Select(u => u.ManagerId)
                    .FirstOrDefaultAsync(ct);
                return managerId.HasValue ? new List<Guid> { managerId.Value } : new List<Guid>();
            }

            case NotificationEvent.ManagerRejected:
            case NotificationEvent.VendorSelected:
            case NotificationEvent.PaymentStarted:
            case NotificationEvent.PaymentCompleted:
            case NotificationEvent.RequestCompleted:
            case NotificationEvent.FinanceRejected:
                // The requester cares about every one of these — it's their request moving.
                return new List<Guid> { request.RequesterId };

            case NotificationEvent.ManagerApproved:
            {
                // Requester (their request cleared Manager) + every Finance user (it just
                // landed in their queue) — the same reasoning as FinanceApproved below, one
                // stage earlier. Missing this half was the original bug: Finance never knew
                // a request was waiting on them until they happened to refresh the queue.
                var recipients = new List<Guid> { request.RequesterId };
                recipients.AddRange(await UsersInRoleIdsAsync(UserRole.Finance, ct));
                return recipients;
            }

            case NotificationEvent.FinanceApproved:
            {
                // Requester (their request cleared Finance) + every Procurement Admin (it
                // just landed in their queue).
                var recipients = new List<Guid> { request.RequesterId };
                recipients.AddRange(await UsersInRoleIdsAsync(UserRole.ProcurementAdmin, ct));
                return recipients;
            }

            case NotificationEvent.PaymentFailed:
            {
                // Procurement Admin needs to retry; the requester should know things stalled too.
                var recipients = new List<Guid> { request.RequesterId };
                recipients.AddRange(await UsersInRoleIdsAsync(UserRole.ProcurementAdmin, ct));
                return recipients;
            }

            default:
                return new List<Guid>();
        }
    }

    private Task<List<Guid>> UsersInRoleIdsAsync(UserRole role, CancellationToken ct) =>
        _db.Users.Where(u => u.Role == role).Select(u => u.Id).ToListAsync(ct);

    private static string BuildMessage(PurchaseRequest request, NotificationEvent evt) => evt switch
    {
        NotificationEvent.RequestSubmitted => $"Purchase request {request.RequestNumber} was submitted and needs your approval.",
        NotificationEvent.ManagerApproved => $"Purchase request {request.RequestNumber} was approved by the manager and is awaiting Finance review.",
        NotificationEvent.ManagerRejected => $"Purchase request {request.RequestNumber} was rejected by your manager.",
        NotificationEvent.FinanceApproved => $"Purchase request {request.RequestNumber} cleared Finance's budget approval.",
        NotificationEvent.FinanceRejected => $"Purchase request {request.RequestNumber} was rejected by Finance.",
        NotificationEvent.VendorSelected => $"A vendor was selected for purchase request {request.RequestNumber}.",
        NotificationEvent.PaymentStarted => $"Payment has started for purchase request {request.RequestNumber}.",
        NotificationEvent.PaymentCompleted => $"Payment succeeded for purchase request {request.RequestNumber}.",
        NotificationEvent.PaymentFailed => $"Payment failed for purchase request {request.RequestNumber} — a retry is needed.",
        NotificationEvent.RequestCompleted => $"Purchase request {request.RequestNumber} is complete.",
        _ => $"Purchase request {request.RequestNumber} was updated.",
    };
}
