using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Gamification
{
    /// <summary>Create a user-defined challenge. Distance target is meters; calories target is kcal.</summary>
    public class CreateCustomChallengeRequestDto
    {
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public CustomChallengeGoalType GoalType { get; set; }

        public double TargetValue { get; set; }

        /// <summary>Optional start; interpreted as UTC (Unspecified from JSON is treated as UTC).</summary>
        public DateTime? StartDate { get; set; }

        /// <summary>Optional end; interpreted as UTC (Unspecified from JSON is treated as UTC).</summary>
        public DateTime? EndDate { get; set; }
    }
}
