using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Gamification
{
    /// <summary>
    /// Result returned by IBadgeEvaluationService.EvaluateBadgeConditionsAsync.
    /// Contains newly awarded badge IDs and evaluation summary counts.
    /// </summary>
    public sealed class BadgeEvaluationResult
    {
        /// <summary>IDs of badges that were newly awarded in this evaluation run.</summary>
        public IReadOnlyList<int> AwardedBadgeIds { get; init; } = [];

        /// <summary>Number of badge conditions that were already satisfied (badge already owned).</summary>
        public int AlreadyOwnedCount { get; init; }

        /// <summary>Number of badge conditions that were checked but not met.</summary>
        public int NotEarnedCount { get; init; }

        /// <summary>True if at least one new badge was awarded.</summary>
        public bool AnyNewBadgeAwarded => AwardedBadgeIds.Count > 0;

        public static BadgeEvaluationResult Empty { get; } = new BadgeEvaluationResult
        {
            AwardedBadgeIds = [],
            AlreadyOwnedCount = 0,
            NotEarnedCount = 0
        };
    }
}
