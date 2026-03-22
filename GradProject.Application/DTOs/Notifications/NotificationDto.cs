using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Notifications;

/// <summary>
/// In-app notification derived from <see cref="GradProject.Domain.Entities.AchievementEvent"/> (BE-6).
/// </summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public AchievementEventType Type { get; set; }
    public DateTime OccurredAt { get; set; }

    public int? BadgeId { get; set; }
    public int? ChallengeId { get; set; }
    public int? TerritoryId { get; set; }
    public int? RunId { get; set; }
    public int? PreviousRank { get; set; }
    public int? CurrentRank { get; set; }
    public string? Metadata { get; set; }

    public DateTime? ReadAtUtc { get; set; }
    public bool IsRead => ReadAtUtc.HasValue;
}
