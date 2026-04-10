using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Gamification
{
    public class ChallengeResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public ChallengeType Type { get; set; }
        public string TypeName { get; set; } = null!;
        public ChallengeMetric Metric { get; set; }
        public string MetricName { get; set; } = null!;
        public double TargetValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RewardPoints { get; set; }
        public bool IsActive { get; set; }

        /// <summary>True when the requesting user has joined this challenge (requires user-scoped list/detail calls).</summary>
        public bool IsParticipating { get; set; }

        /// <summary>Set when <see cref="IsParticipating"/> is true.</summary>
        public DateTime? JoinedAt { get; set; }

        public bool Completed { get; set; }

        public DateTime? CompletedAt { get; set; }

        public long ProgressDistanceMeters { get; set; }

        public int ProgressCalories { get; set; }

        /// <summary>0–100 when participating; otherwise 0.</summary>
        public double ProgressPercent { get; set; }

        public bool IsCustom { get; set; }

        public int? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}

