using GradProject.Domain.Enums;

namespace GradProject.Domain.Entities;

/// <summary>
/// Persisted achievement event — the domain/audit feed for all significant user milestones.
/// Acts as the source-of-truth event log that BE-6 Notification API and AI-4 commentary can
/// consume from.
///
/// Idempotency: DeduplicationKey has a unique DB index. Duplicate domain actions do not produce
/// duplicate rows — the publisher catches the unique-constraint violation and silently skips.
///
/// Nullable fields are only populated when relevant to the event type:
///   - BadgeEarned:        BadgeId, (RunId for PersonalBest badge)
///   - PersonalBest:       RunId
///   - ChallengeCompleted: ChallengeId
///   - LeaderboardClimbed: ChallengeId (null = global), PreviousRank, CurrentRank
///   - Territory*:         TerritoryId, RunId
/// </summary>
public class AchievementEvent
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public AchievementEventType Type { get; set; }
    public DateTime OccurredAt { get; set; }

    public int? BadgeId { get; set; }
    public int? ChallengeId { get; set; }
    public int? TerritoryId { get; set; }
    public int? RunId { get; set; }
    public int? PreviousRank { get; set; }
    public int? CurrentRank { get; set; }

    /// <summary>
    /// Optional JSON payload for extra context (e.g. score, coverage ratio).
    /// Stored as jsonb. Kept untyped so the schema stays stable while the payload evolves.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Stable, deterministic key used for deduplication.
    /// Format is defined per-event-type in AchievementEventService.
    /// </summary>
    public string DeduplicationKey { get; set; } = null!;

    /// <summary>
    /// When set, the in-app notification for this event is considered read (UTC).
    /// </summary>
    public DateTime? ReadAtUtc { get; set; }

    public User? User { get; set; }
}
