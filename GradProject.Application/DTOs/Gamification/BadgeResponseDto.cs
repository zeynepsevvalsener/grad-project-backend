using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Gamification
{
    public class BadgeResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public BadgeType Type { get; set; }
        public string TypeName { get; set; } = null!;
        public string? IconUrl { get; set; }
        public int PointsReward { get; set; }
        public bool IsActive { get; set; }
    }
}

