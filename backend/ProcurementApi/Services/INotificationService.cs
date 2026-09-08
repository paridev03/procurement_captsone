using ProcurementApi.DTOs;

namespace ProcurementApi.Services;

/// <summary>
/// Owns exactly one responsibility: storing and retrieving delivered notifications. It has
/// no opinion about *who* should be notified for a given business event or *what* the
/// message should say — that decision lives in NotificationObserver. Splitting these keeps
/// each class changeable for its own reason only (SRP): recipient/message rules can change
/// without touching persistence, and the storage/query shape can change (e.g. adding
/// pagination) without touching a single business rule.
/// </summary>
public interface INotificationService
{
    Task CreateAsync(Guid userId, Domain.Enums.NotificationEvent evt, string message, Guid? purchaseRequestId, CancellationToken ct = default);
    Task<List<NotificationDto>> GetForUserAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
}
