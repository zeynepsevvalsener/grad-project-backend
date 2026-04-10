using GradProject.Domain.Enums;

namespace GradProject.Domain.Entities
{
    public class Challenge
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public ChallengeType Type { get; set; }
        public ChallengeMetric Metric { get; set; }
        public double TargetValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RewardPoints { get; set; }
        public bool IsActive { get; set; }

        /// <summary>True when created via user-facing custom challenge API (not admin seed).</summary>
        public bool IsCustom { get; set; }

        /// <summary>Creator user id for custom challenges; null for system/admin challenges.</summary>
        public int? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public User? CreatedByUser { get; set; }
    }
}

