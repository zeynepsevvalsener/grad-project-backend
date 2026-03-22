using GradProject.Application.DTOs.Common;
using GradProject.Application.DTOs.Notifications;

namespace GradProject.Application.Interfaces;

public interface INotificationService
{
    Task<PagedResultDto<NotificationDto>> GetPagedAsync(
        int userId,
        int page,
        int pageSize,
        bool unreadOnly,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default);

    /// <summary>Returns false if the notification does not exist or belongs to another user.</summary>
    Task<bool> MarkAsReadAsync(int userId, Guid notificationId, CancellationToken ct = default);

    /// <summary>Number of rows updated (unread → read).</summary>
    Task<int> MarkAllAsReadAsync(int userId, CancellationToken ct = default);
}
