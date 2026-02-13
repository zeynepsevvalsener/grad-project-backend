using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Gamification
{
    public class UpdateChallengeRequestDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public ChallengeType Type { get; set; }
        public ChallengeMetric Metric { get; set; }
        public double TargetValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RewardPoints { get; set; }
        public bool IsActive { get; set; }
    }
}

