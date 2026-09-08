using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementApi.DTOs;
using ProcurementApi.Services;

namespace ProcurementApi.Controllers;

/// <summary>Notifications are always scoped to the caller (via ICurrentUser) — every role
/// gets the same bell, just with whatever NotificationObserver decided to send them.</summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;
    private readonly ICurrentUser _currentUser;

    public NotificationsController(INotificationService notifications, ICurrentUser currentUser)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetMine(CancellationToken ct) =>
        Ok(await _notifications.GetForUserAsync(_currentUser.Id, ct));

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount(CancellationToken ct) =>
        Ok(new UnreadCountDto(await _notifications.GetUnreadCountAsync(_currentUser.Id, ct)));

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        await _notifications.MarkAsReadAsync(_currentUser.Id, id, ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        await _notifications.MarkAllAsReadAsync(_currentUser.Id, ct);
        return NoContent();
    }
}
