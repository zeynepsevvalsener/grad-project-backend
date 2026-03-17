using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Gamification
{
    /// <summary>
    /// Badge earned by a user, shaped for API/UI (includes earned date in UTC).
    /// </summary>
    public class UserBadgeResponseDto
    {
        public int BadgeId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public BadgeType Type { get; set; }
        public string? IconUrl { get; set; }
        public int PointsReward { get; set; }
        public DateTime EarnedAtUtc { get; set; }
    }
}
