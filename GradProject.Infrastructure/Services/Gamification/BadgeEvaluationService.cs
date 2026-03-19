using System.Globalization;
using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GradProject.Infrastructure.Services.Gamification
{
    /// <summary>
    /// Badge rule definitions:
    ///
    /// FirstRun          — User has at least 1 run in RunningActivities. One-time.
    ///
    /// Streak (Weekly)   — User has runs in at least MinimumStreakWeeks=2 consecutive ISO
    ///                     calendar weeks (Mon–Sun) anywhere in their history.
    ///                     "Consecutive" means adjacent ISO year-weeks with no gap week.
    ///                     Threshold is a named constant — easy to adjust. One-time.
    ///
    /// PersonalBest      — User has at least one qualifying run (DistanceMeters > 500 m) AND
    ///                     the most recently persisted qualifying run (by CreatedAt DESC) has
    ///                     the best or equal-best pace (MovingTimeSeconds / DistanceMeters)
    ///                     across all of the user's qualifying runs. "First run = first PB"
    ///                     is intentional — the badge is awarded once, on first achievement.
    ///                     One-time.
    ///
    /// TerritoryFirstClaim — User has any TerritoryOwnershipHistory row with
    ///                       ActionType==Claim and NewOwnerUserId==userId. One-time.
    ///
    /// TerritoryDefender   — User has any TerritoryOwnershipHistory row with
    ///                       ActionType==Defend and NewOwnerUserId==userId. One-time.
    ///
    /// TerritoryConqueror  — User has any TerritoryOwnershipHistory row with
    ///                       ActionType==Transfer and NewOwnerUserId==userId (took territory
    ///                       from another user). One-time.
    ///
    /// ChallengeCompletion — User has any UserChallenge with Completed==true. One-time.
    ///
    /// All badges are looked up by BadgeType from the Badges table — no IDs are hardcoded.
    /// If no active badge row exists for a given type, that rule is silently skipped.
    ///
    /// TODO (BE-5): After awarding, publish a badge-earned event via IBadgeEventPublisher
    /// (not yet implemented). See existing NoOpLeaderboardEventPublisher pattern.
    /// </summary>
    public class BadgeEvaluationService : IBadgeEvaluationService
    {
        // A "streak" requires runs in this many consecutive ISO calendar weeks.
        private const int MinimumStreakWeeks = 2;

        // Minimum run distance to qualify for Personal Best evaluation.
        private const double MinPbDistanceMeters = 500.0;

        private readonly AppDbContext _db;
        private readonly IBadgeService _badgeService;
        private readonly ILogger<BadgeEvaluationService> _logger;

        public BadgeEvaluationService(
            AppDbContext db,
            IBadgeService badgeService,
            ILogger<BadgeEvaluationService> logger)
        {
            _db = db;
            _badgeService = badgeService;
            _logger = logger;
        }

        public async Task<BadgeEvaluationResult> EvaluateBadgeConditionsAsync(
            int userId,
            CancellationToken ct = default)
        {
            var userExists = await _db.Users.AnyAsync(u => u.Id == userId, ct);
            if (!userExists)
            {
                _logger.LogWarning("EvaluateBadgeConditions: user {UserId} not found — skipping", userId);
                return BadgeEvaluationResult.Empty;
            }

            var awarded = new List<int>();
            int alreadyOwned = 0;
            int notEarned = 0;

            async Task TryAward(BadgeType type, bool conditionMet)
            {
                if (!conditionMet)
                {
                    notEarned++;
                    return;
                }

                var badge = await _db.Badges
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Type == type && b.IsActive, ct);

                if (badge == null)
                {
                    _logger.LogDebug(
                        "EvaluateBadgeConditions: no active badge found for type {BadgeType} — skipping user {UserId}",
                        type, userId);
                    notEarned++;
                    return;
                }

                var result = await _badgeService.AwardBadgeAsync(userId, badge.Id, ct);

                switch (result)
                {
                    case AwardBadgeResult.Created:
                        awarded.Add(badge.Id);
                        _logger.LogInformation(
                            "Badge awarded: type={BadgeType} badgeId={BadgeId} userId={UserId}",
                            type, badge.Id, userId);
                        break;
                    case AwardBadgeResult.AlreadyExists:
                        alreadyOwned++;
                        break;
                    default:
                        _logger.LogWarning(
                            "AwardBadgeAsync returned {Result} for type={BadgeType} userId={UserId}",
                            result, type, userId);
                        notEarned++;
                        break;
                }
            }

            // Run-based badges
            await EvaluateRunBadgesAsync(userId, TryAward, ct);

            // Territory-based badges
            await EvaluateTerritoryBadgesAsync(userId, TryAward, ct);

            // Challenge-based badges
            await EvaluateChallengeBadgesAsync(userId, TryAward, ct);

            return new BadgeEvaluationResult
            {
                AwardedBadgeIds = awarded,
                AlreadyOwnedCount = alreadyOwned,
                NotEarnedCount = notEarned
            };
        }

        // -------------------------------------------------------------------------
        // Run-based badge evaluators
        // -------------------------------------------------------------------------

        private async Task EvaluateRunBadgesAsync(
            int userId,
            Func<BadgeType, bool, Task> tryAward,
            CancellationToken ct)
        {
            var runs = await _db.RunningActivities
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .Select(r => new
                {
                    r.RunDate,
                    r.DistanceMeters,
                    r.MovingTimeSeconds,
                    r.CreatedAt
                })
                .ToListAsync(ct);

            // First Run
            await tryAward(BadgeType.FirstRun, runs.Count > 0);

            // Weekly Streak
            await tryAward(BadgeType.Streak, HasWeeklyStreak(runs.Select(r => r.RunDate)));

            // Personal Best
            var qualifyingRuns = runs
                .Where(r => r.DistanceMeters > MinPbDistanceMeters && r.MovingTimeSeconds > 0)
                .ToList();

            bool hasPersonalBest = false;
            if (qualifyingRuns.Count > 0)
            {
                double minPace = qualifyingRuns.Min(r => r.MovingTimeSeconds / r.DistanceMeters);
                var latestRun = qualifyingRuns.OrderByDescending(r => r.CreatedAt).First();
                double latestPace = latestRun.MovingTimeSeconds / latestRun.DistanceMeters;
                // Award when latest run matches or beats the all-time best pace.
                hasPersonalBest = latestPace <= minPace;
            }

            await tryAward(BadgeType.PersonalBest, hasPersonalBest);
        }

        /// <summary>
        /// Returns true if the given run dates contain at least MinimumStreakWeeks consecutive
        /// ISO calendar weeks. Consecutive means adjacent weeks (no missing week between them).
        /// </summary>
        private static bool HasWeeklyStreak(IEnumerable<DateOnly> runDates)
        {
            // Collect distinct ISO year-week keys and sort them.
            var weeks = runDates
                .Select(d =>
                {
                    var dt = d.ToDateTime(TimeOnly.MinValue);
                    int year = ISOWeek.GetYear(dt);
                    int week = ISOWeek.GetWeekOfYear(dt);
                    // Encode as a single comparable integer: year * 100 + week (max 53 weeks/year).
                    return year * 100 + week;
                })
                .Distinct()
                .OrderBy(w => w)
                .ToList();

            if (weeks.Count < MinimumStreakWeeks)
                return false;

            int consecutiveCount = 1;
            for (int i = 1; i < weeks.Count; i++)
            {
                int prev = weeks[i - 1];
                int curr = weeks[i];

                // Convert encoded keys back to (year, week) to determine whether the two
                // weeks are truly adjacent, handling year-boundary transitions correctly.
                int prevYear = prev / 100, prevWeek = prev % 100;
                int currYear = curr / 100, currWeek = curr % 100;

                bool adjacent = IsAdjacentWeek(prevYear, prevWeek, currYear, currWeek);

                if (adjacent)
                {
                    consecutiveCount++;
                    if (consecutiveCount >= MinimumStreakWeeks)
                        return true;
                }
                else
                {
                    consecutiveCount = 1;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when (currYear, currWeek) is exactly one ISO week after (prevYear, prevWeek).
        /// Handles year-end boundaries (e.g. week 52/53 → week 1 of next year).
        /// </summary>
        private static bool IsAdjacentWeek(int prevYear, int prevWeek, int currYear, int currWeek)
        {
            if (currYear == prevYear)
                return currWeek == prevWeek + 1;

            if (currYear == prevYear + 1 && currWeek == 1)
            {
                int weeksInPrevYear = ISOWeek.GetWeeksInYear(prevYear);
                return prevWeek == weeksInPrevYear;
            }

            return false;
        }

        // -------------------------------------------------------------------------
        // Territory-based badge evaluators
        // -------------------------------------------------------------------------

        private async Task EvaluateTerritoryBadgesAsync(
            int userId,
            Func<BadgeType, bool, Task> tryAward,
            CancellationToken ct)
        {
            var historyTypes = await _db.TerritoryOwnershipHistories
                .AsNoTracking()
                .Where(h => h.NewOwnerUserId == userId)
                .Select(h => h.ActionType)
                .Distinct()
                .ToListAsync(ct);

            var actionSet = new HashSet<OwnershipActionType>(historyTypes);

            await tryAward(BadgeType.TerritoryFirstClaim, actionSet.Contains(OwnershipActionType.Claim));
            await tryAward(BadgeType.TerritoryDefender, actionSet.Contains(OwnershipActionType.Defend));
            await tryAward(BadgeType.TerritoryConqueror, actionSet.Contains(OwnershipActionType.Transfer));
        }

        // -------------------------------------------------------------------------
        // Challenge-based badge evaluators
        // -------------------------------------------------------------------------

        private async Task EvaluateChallengeBadgesAsync(
            int userId,
            Func<BadgeType, bool, Task> tryAward,
            CancellationToken ct)
        {
            var hasCompletedChallenge = await _db.UserChallenges
                .AsNoTracking()
                .AnyAsync(uc => uc.UserId == userId && uc.Completed, ct);

            await tryAward(BadgeType.ChallengeCompletion, hasCompletedChallenge);
        }
    }
}
