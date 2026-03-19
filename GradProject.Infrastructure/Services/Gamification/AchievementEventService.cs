using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GradProject.Infrastructure.Services.Gamification;

/// <summary>
/// Persists achievement events to the AchievementEvents audit table.
/// Provides idempotent delivery via a unique DeduplicationKey constraint —
/// duplicate calls with the same key are silently ignored.
///
/// DeduplicationKey conventions (callers must use <see cref="AchievementDeduplicationKeys"/>
/// helper methods — never build key strings inline).
///
/// Important: This service calls SaveChangesAsync on the shared AppDbContext.
/// Callers must ensure the context has no unintended pending changes before calling PublishAsync.
/// In practice all callers invoke PublishAsync AFTER their own SaveChangesAsync, so the context
/// is always clean at the point of publishing.
/// </summary>
public class AchievementEventService : IAchievementEventPublisher
{
    // Postgres unique-constraint violation error code.
    private const string PostgresUniqueViolationCode = "23505";

    private readonly AppDbContext _db;
    private readonly ILogger<AchievementEventService> _logger;

    public AchievementEventService(AppDbContext db, ILogger<AchievementEventService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task PublishAsync(AchievementEventDto evt, CancellationToken ct = default)
    {
        // Fast-path dedup check (non-atomic, but the unique constraint is the real gate).
        var exists = await _db.AchievementEvents
            .AsNoTracking()
            .AnyAsync(e => e.DeduplicationKey == evt.DeduplicationKey, ct);

        if (exists)
        {
            _logger.LogDebug(
                "Achievement event skipped (duplicate): Type={Type} UserId={UserId} Key={Key}",
                evt.Type, evt.UserId, evt.DeduplicationKey);
            return;
        }

        var entity = new AchievementEvent
        {
            Id = evt.Id == Guid.Empty ? Guid.NewGuid() : evt.Id,
            UserId = evt.UserId,
            Type = evt.Type,
            OccurredAt = evt.OccurredAt == default ? DateTime.UtcNow : evt.OccurredAt,
            BadgeId = evt.BadgeId,
            ChallengeId = evt.ChallengeId,
            TerritoryId = evt.TerritoryId,
            RunId = evt.RunId,
            PreviousRank = evt.PreviousRank,
            CurrentRank = evt.CurrentRank,
            Metadata = evt.Metadata,
            DeduplicationKey = evt.DeduplicationKey
        };

        _db.AchievementEvents.Add(entity);

        try
        {
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Achievement event persisted: Type={Type} UserId={UserId} Key={Key}",
                evt.Type, evt.UserId, evt.DeduplicationKey);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Concurrent duplicate — the unique constraint protected us. Detach and move on.
            _db.Entry(entity).State = EntityState.Detached;
            _logger.LogDebug(
                "Achievement event deduped (concurrent): Type={Type} UserId={UserId} Key={Key}",
                evt.Type, evt.UserId, evt.DeduplicationKey);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        while (inner != null)
        {
            if (inner is PostgresException pg && pg.SqlState == PostgresUniqueViolationCode)
                return true;
            inner = inner.InnerException;
        }
        return false;
    }
}
