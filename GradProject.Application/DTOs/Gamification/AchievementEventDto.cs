using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Gamification;

/// <summary>
/// Payload for publishing an achievement event through IAchievementEventPublisher.
/// Also serves as the stable contract for BE-6 Notification API and AI-4 commentary.
///
/// Id should be pre-populated by the caller (Guid.NewGuid()).
/// OccurredAt should be UTC; defaults to UtcNow in the publisher if not set.
/// DeduplicationKey must be set by the caller — see AchievementEventService for format conventions.
/// </summary>
public class AchievementEventDto
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
    public string? Metadata { get; set; }

    /// <summary>
    /// Deterministic key for idempotent delivery.
    /// Duplicate calls with the same key are silently ignored by the publisher.
    /// </summary>
    public string DeduplicationKey { get; set; } = null!;
}
