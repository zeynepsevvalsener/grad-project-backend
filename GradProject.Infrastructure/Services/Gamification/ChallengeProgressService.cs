using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GradProject.Infrastructure.Services.Gamification
{
    public class ChallengeProgressService : IChallengeProgressService
    {
        private readonly AppDbContext _db;
        private readonly IBadgeEvaluationService _badgeEvaluationService;
        private readonly IAchievementEventPublisher _achievementPublisher;
        private readonly ILogger<ChallengeProgressService> _logger;

        public ChallengeProgressService(
            AppDbContext db,
            IBadgeEvaluationService badgeEvaluationService,
            IAchievementEventPublisher achievementPublisher,
            ILogger<ChallengeProgressService> logger)
        {
            _db = db;
            _badgeEvaluationService = badgeEvaluationService;
            _achievementPublisher = achievementPublisher;
            _logger = logger;
        }

        public async Task UpdateAfterRunSaved(int runId, CancellationToken ct = default)
        {
            try
            {
                var run = await _db.RunningActivities
                    .FirstOrDefaultAsync(r => r.Id == runId, ct);

                if (run == null)
                {
                    _logger.LogWarning("RunningActivity with Id {RunId} not found for challenge progress update", runId);
                    return;
                }

                if (run.UserId <= 0)
                {
                    _logger.LogWarning("RunningActivity {RunId} has invalid UserId {UserId}", runId, run.UserId);
                    return;
                }

                if (run.DistanceMeters <= 0)
                {
                    _logger.LogDebug("RunningActivity {RunId} has zero or negative distance, skipping challenge progress update", runId);
                    return;
                }

                var now = DateTime.UtcNow;

                var activeUserChallenges = await _db.UserChallenges
                    .Include(uc => uc.Challenge)
                    .Where(uc =>
                        uc.UserId == run.UserId &&
                        uc.Completed == false &&
                        uc.Challenge.IsActive == true &&
                        uc.Challenge.Metric == ChallengeMetric.Distance &&
                        uc.Challenge.Type == ChallengeType.Running &&
                        now >= uc.Challenge.StartDate &&
                        now <= uc.Challenge.EndDate)
                    .ToListAsync(ct);

                if (activeUserChallenges.Count == 0)
                {
                    _logger.LogDebug("No active distance-based running challenges found for user {UserId} after run {RunId}", run.UserId, runId);
                    return;
                }

                var newlyCompleted = new List<(int UserId, int ChallengeId)>();

                foreach (var userChallenge in activeUserChallenges)
                {
                    userChallenge.ProgressDistanceMeters += (long)run.DistanceMeters;
                    userChallenge.LastUpdatedAt = now;

                    if (userChallenge.ProgressDistanceMeters >= (long)userChallenge.Challenge.TargetValue)
                    {
                        userChallenge.Completed = true;
                        userChallenge.CompletedAt = now;
                        newlyCompleted.Add((run.UserId, userChallenge.ChallengeId));

                        _logger.LogInformation(
                            "User {UserId} completed challenge {ChallengeId} (Progress: {Progress}m / Target: {Target}m)",
                            run.UserId, userChallenge.ChallengeId,
                            userChallenge.ProgressDistanceMeters, userChallenge.Challenge.TargetValue);
                    }
                }

                await _db.SaveChangesAsync(ct);

                // Post-save: emit ChallengeCompleted events and evaluate badges.
                // These are fire-and-forget relative to the run-save flow.
                if (newlyCompleted.Count > 0)
                {
                    foreach (var (uid, challengeId) in newlyCompleted)
                    {
                        await EmitChallengeCompletedAsync(uid, challengeId, ct);
                    }

                    try
                    {
                        await _badgeEvaluationService.EvaluateBadgeConditionsAsync(run.UserId, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to evaluate badge conditions for user {UserId} after challenge completion", run.UserId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating challenge progress for run {RunId}", runId);
            }
        }

        public async Task UpdateAfterNutritionSaved(int userId, int totalCalories, DateOnly date, CancellationToken ct = default)
        {
            try
            {
                if (userId <= 0)
                {
                    _logger.LogWarning("Invalid UserId {UserId} for nutrition challenge progress update", userId);
                    return;
                }

                if (totalCalories < 0)
                {
                    _logger.LogDebug("Negative calories for user {UserId}, skipping challenge progress update", userId);
                    return;
                }

                var now = DateTime.UtcNow;

                var activeUserChallenges = await _db.UserChallenges
                    .Include(uc => uc.Challenge)
                    .Where(uc =>
                        uc.UserId == userId &&
                        uc.Completed == false &&
                        uc.Challenge.IsActive == true &&
                        uc.Challenge.Metric == ChallengeMetric.Calories &&
                        uc.Challenge.Type == ChallengeType.Nutrition &&
                        now >= uc.Challenge.StartDate &&
                        now <= uc.Challenge.EndDate)
                    .ToListAsync(ct);

                if (activeUserChallenges.Count == 0)
                {
                    _logger.LogDebug("No active calories-based nutrition challenges found for user {UserId}", userId);
                    return;
                }

                var cacheKey = $"{userId}_{date:yyyy-MM-dd}";
                var lastProcessedCalories = GetLastProcessedCalories(cacheKey);
                var caloriesToAdd = totalCalories - lastProcessedCalories;

                if (caloriesToAdd <= 0)
                {
                    _logger.LogDebug(
                        "No new calories to add for user {UserId} on date {Date} (Total: {Total}, LastProcessed: {Last})",
                        userId, date, totalCalories, lastProcessedCalories);
                    return;
                }

                var newlyCompleted = new List<(int UserId, int ChallengeId)>();

                foreach (var userChallenge in activeUserChallenges)
                {
                    userChallenge.ProgressCalories += caloriesToAdd;
                    userChallenge.LastUpdatedAt = now;

                    if (userChallenge.ProgressCalories >= (int)userChallenge.Challenge.TargetValue)
                    {
                        userChallenge.Completed = true;
                        userChallenge.CompletedAt = now;
                        newlyCompleted.Add((userId, userChallenge.ChallengeId));

                        _logger.LogInformation(
                            "User {UserId} completed challenge {ChallengeId} (Progress: {Progress} cal / Target: {Target} cal)",
                            userId, userChallenge.ChallengeId,
                            userChallenge.ProgressCalories, userChallenge.Challenge.TargetValue);
                    }
                }

                await _db.SaveChangesAsync(ct);

                SetLastProcessedCalories(cacheKey, totalCalories);

                // Post-save: emit ChallengeCompleted events and evaluate badges.
                if (newlyCompleted.Count > 0)
                {
                    foreach (var (uid, challengeId) in newlyCompleted)
                    {
                        await EmitChallengeCompletedAsync(uid, challengeId, ct);
                    }

                    try
                    {
                        await _badgeEvaluationService.EvaluateBadgeConditionsAsync(userId, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to evaluate badge conditions for user {UserId} after nutrition challenge completion", userId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating challenge progress for nutrition data for user {UserId} on date {Date}", userId, date);
            }
        }

        // -------------------------------------------------------------------------
        // Event emission
        // -------------------------------------------------------------------------

        private async Task EmitChallengeCompletedAsync(int userId, int challengeId, CancellationToken ct)
        {
            try
            {
                await _achievementPublisher.PublishAsync(new AchievementEventDto
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = AchievementEventType.ChallengeCompleted,
                    OccurredAt = DateTime.UtcNow,
                    ChallengeId = challengeId,
                    DeduplicationKey = AchievementDeduplicationKeys.ChallengeCompleted(userId, challengeId)
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to emit ChallengeCompleted event for userId={UserId} challengeId={ChallengeId}",
                    userId, challengeId);
            }
        }

        // -------------------------------------------------------------------------
        // In-memory delta-calorie cache
        // -------------------------------------------------------------------------

        // Simple in-memory cache for tracking last processed calories per (userId, date).
        // Not suitable for distributed deployments; replace with Redis or a DB table for production scale.
        private static readonly Dictionary<string, int> _lastProcessedCaloriesCache = new();
        private static readonly object _cacheLock = new();

        private int GetLastProcessedCalories(string cacheKey)
        {
            lock (_cacheLock)
            {
                return _lastProcessedCaloriesCache.TryGetValue(cacheKey, out var value) ? value : 0;
            }
        }

        private void SetLastProcessedCalories(string cacheKey, int totalCalories)
        {
            lock (_cacheLock)
            {
                _lastProcessedCaloriesCache[cacheKey] = totalCalories;
            }
        }
    }
}
