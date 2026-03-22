using GradProject.Application.DTOs.Common;
using GradProject.Application.DTOs.Notifications;
using GradProject.Application.Interfaces;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResultDto<NotificationDto>> GetPagedAsync(
        int userId,
        int page,
        int pageSize,
        bool unreadOnly,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.AchievementEvents.AsNoTracking().Where(e => e.UserId == userId);
        if (unreadOnly)
            query = query.Where(e => e.ReadAtUtc == null);

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (totalPages > 0 && page > totalPages)
            page = totalPages;

        var skip = (page - 1) * pageSize;
        var rows = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(ct);
        var items = rows.Select(Map).ToList();

        return new PagedResultDto<NotificationDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default)
    {
        return _db.AchievementEvents.AsNoTracking()
            .CountAsync(e => e.UserId == userId && e.ReadAtUtc == null, ct);
    }

    public async Task<bool> MarkAsReadAsync(int userId, Guid notificationId, CancellationToken ct = default)
    {
        var row = await _db.AchievementEvents
            .FirstOrDefaultAsync(e => e.Id == notificationId && e.UserId == userId, ct);
        if (row == null)
            return false;

        if (row.ReadAtUtc != null)
            return true;

        row.ReadAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(int userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.AchievementEvents
            .Where(e => e.UserId == userId && e.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ReadAtUtc, now), ct);
    }

    private static NotificationDto Map(AchievementEvent e) => new()
    {
        Id = e.Id,
        Type = e.Type,
        OccurredAt = e.OccurredAt,
        BadgeId = e.BadgeId,
        ChallengeId = e.ChallengeId,
        TerritoryId = e.TerritoryId,
        RunId = e.RunId,
        PreviousRank = e.PreviousRank,
        CurrentRank = e.CurrentRank,
        Metadata = e.Metadata,
        ReadAtUtc = e.ReadAtUtc
    };
}
