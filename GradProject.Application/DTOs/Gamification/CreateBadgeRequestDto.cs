using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Gamification
{
    public class CreateBadgeRequestDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public BadgeType Type { get; set; }
        public string? IconUrl { get; set; }
        public int PointsReward { get; set; }
        public bool IsActive { get; set; }
    }
}

