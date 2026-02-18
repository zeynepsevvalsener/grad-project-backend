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
        private readonly ILogger<ChallengeProgressService> _logger;

        public ChallengeProgressService(AppDbContext db, ILogger<ChallengeProgressService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task UpdateAfterRunSaved(int runId, CancellationToken ct = default)
        {
            try
            {
                // Load RunningActivity by runId
                var run = await _db.RunningActivities
                    .FirstOrDefaultAsync(r => r.Id == runId, ct);

                // Guard: null check, UserId check, DistanceMeters > 0
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

                // Query active UserChallenges
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

                // For each UserChallenge, update progress
                foreach (var userChallenge in activeUserChallenges)
                {
                    // Increment ProgressDistanceMeters (cast double to long)
                    userChallenge.ProgressDistanceMeters += (long)run.DistanceMeters;
                    userChallenge.LastUpdatedAt = now;

                    // Check completion: if ProgressDistanceMeters >= Challenge.TargetValue
                    if (userChallenge.ProgressDistanceMeters >= (long)userChallenge.Challenge.TargetValue)
                    {
                        userChallenge.Completed = true;
                        userChallenge.CompletedAt = now;
                        _logger.LogInformation(
                            "User {UserId} completed challenge {ChallengeId} (Progress: {Progress}m / Target: {Target}m)",
                            run.UserId, userChallenge.ChallengeId, userChallenge.ProgressDistanceMeters, userChallenge.Challenge.TargetValue);
                    }
                }

                // SaveChanges (with error handling - log and swallow, don't throw)
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - don't break run save flow
                _logger.LogError(ex, "Error updating challenge progress for run {RunId}", runId);
            }
        }

        public async Task UpdateAfterNutritionSaved(int userId, int totalCalories, DateOnly date, CancellationToken ct = default)
        {
            try
            {
                // Guard: userId check, totalCalories >= 0
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

                // Query active UserChallenges for nutrition challenges
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

                // Calculate the increment: difference between current total and what was last processed
                // Use a static dictionary to track last processed calories per (userId, date)
                // This ensures we only add new calories, not re-add calories when aggregate is called multiple times
                // Note: This is not ideal for distributed systems but works for single-instance deployments
                // For production, consider using a distributed cache (Redis) or a database table
                var cacheKey = $"{userId}_{date:yyyy-MM-dd}";
                var lastProcessedCalories = GetLastProcessedCalories(cacheKey);
                var caloriesToAdd = totalCalories - lastProcessedCalories;
                
                if (caloriesToAdd <= 0)
                {
                    _logger.LogDebug("No new calories to add for user {UserId} on date {Date} (Total: {Total}, LastProcessed: {Last})", 
                        userId, date, totalCalories, lastProcessedCalories);
                    return;
                }

                // For each UserChallenge, update progress
                foreach (var userChallenge in activeUserChallenges)
                {
                    // Increment ProgressCalories by the new calories
                    userChallenge.ProgressCalories += caloriesToAdd;
                    userChallenge.LastUpdatedAt = now;

                    // Check completion: if ProgressCalories >= Challenge.TargetValue
                    if (userChallenge.ProgressCalories >= (int)userChallenge.Challenge.TargetValue)
                    {
                        userChallenge.Completed = true;
                        userChallenge.CompletedAt = now;
                        _logger.LogInformation(
                            "User {UserId} completed challenge {ChallengeId} (Progress: {Progress} cal / Target: {Target} cal)",
                            userId, userChallenge.ChallengeId, userChallenge.ProgressCalories, userChallenge.Challenge.TargetValue);
                    }
                }

                // SaveChanges (with error handling - log and swallow, don't throw)
                await _db.SaveChangesAsync(ct);
                
                // Update cache with last processed calories
                SetLastProcessedCalories(cacheKey, totalCalories);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - don't break nutrition save flow
                _logger.LogError(ex, "Error updating challenge progress for nutrition data for user {UserId} on date {Date}", userId, date);
            }
        }

        // Simple in-memory cache for tracking last processed calories per (userId, date)
        // Note: This is not ideal for distributed systems but works for single-instance deployments
        // For production, consider using a distributed cache (Redis) or a database table
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

