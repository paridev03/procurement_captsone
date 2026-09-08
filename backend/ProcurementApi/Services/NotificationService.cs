using Microsoft.EntityFrameworkCore;
using ProcurementApi.Data;
using ProcurementApi.Domain.Entities;
using ProcurementApi.Domain.Enums;
using ProcurementApi.DTOs;

namespace ProcurementApi.Services;

/// <summary>EF-backed INotificationService — see the interface for why this stays a thin
/// persistence layer with no recipient/message logic of its own.</summary>
public class NotificationService : INotificationService
{
    private readonly ProcurementDbContext _db;

    public NotificationService(ProcurementDbContext db)
    {
        _db = db;
    }

    public async Task CreateAsync(Guid userId, NotificationEvent evt, string message, Guid? purchaseRequestId, CancellationToken ct = default)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Event = evt,
            Message = message,
            PurchaseRequestId = purchaseRequestId,
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<NotificationDto>> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto(
                n.Id, n.Event.ToString(), n.Message, n.PurchaseRequestId,
                n.PurchaseRequest != null ? n.PurchaseRequest.RequestNumber : null,
                n.IsRead, n.CreatedAt))
            .ToListAsync(ct);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct)
            ?? throw new NotFoundException("Notification not found.");
        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}
