using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GradProject.Application.DTOs.Common;
using GradProject.Application.DTOs.Notifications;
using GradProject.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    /// <summary>Lists achievement-based notifications for the current user (newest first).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<NotificationDto>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false,
        CancellationToken ct = default)
    {
        var userId = GetUserIdOrThrow();
        var result = await _notifications.GetPagedAsync(userId, page, pageSize, unreadOnly, ct);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountResponse>> GetUnreadCount(CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();
        var count = await _notifications.GetUnreadCountAsync(userId, ct);
        return Ok(new UnreadCountResponse { UnreadCount = count });
    }

    [HttpPatch("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();
        var ok = await _notifications.MarkAsReadAsync(userId, notificationId, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<MarkAllReadResponse>> MarkAllAsRead(CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();
        var updated = await _notifications.MarkAllAsReadAsync(userId, ct);
        return Ok(new MarkAllReadResponse { UpdatedCount = updated });
    }

    private int GetUserIdOrThrow()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
            throw new UnauthorizedAccessException("Invalid token.");
        return userId;
    }

    public sealed class UnreadCountResponse
    {
        public int UnreadCount { get; set; }
    }

    public sealed class MarkAllReadResponse
    {
        public int UpdatedCount { get; set; }
    }
}
