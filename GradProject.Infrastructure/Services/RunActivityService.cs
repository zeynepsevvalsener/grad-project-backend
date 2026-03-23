using GradProject.Application.DTOs.Common;
using GradProject.Application.Interfaces;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Application.Interfaces.Geometry;
using GradProject.Application.Services.Polyline;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services.Geometry;
using GradProject.Infrastructure.Services.Strava;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GradProject.Infrastructure.Services
{
    public class RunActivityService : IRunActivityService
    {
        private readonly AppDbContext _db;
        private readonly StravaApiService _stravaApiService;
        private readonly ILogger<RunActivityService> _logger;
        private readonly PolylineDecoder _polylineDecoder;
        private readonly GeometryConverter _geometryConverter;
        private readonly IBoundingBoxService _boundingBoxService;
        private readonly IConvexHullService _convexHullService;
        private readonly IChallengeProgressService _challengeProgressService;
        private readonly IBadgeEvaluationService _badgeEvaluationService;

        public RunActivityService(
            AppDbContext db,
            StravaApiService stravaApiService,
            ILogger<RunActivityService> logger,
            PolylineDecoder polylineDecoder,
            GeometryConverter geometryConverter,
            IBoundingBoxService boundingBoxService,
            IConvexHullService convexHullService,
            IChallengeProgressService challengeProgressService,
            IBadgeEvaluationService badgeEvaluationService)
        {
            _db = db;
            _stravaApiService = stravaApiService;
            _logger = logger;
            _polylineDecoder = polylineDecoder;
            _geometryConverter = geometryConverter;
            _boundingBoxService = boundingBoxService;
            _convexHullService = convexHullService;
            _challengeProgressService = challengeProgressService;
            _badgeEvaluationService = badgeEvaluationService;
        }

        public async Task<FetchLatestRunResult> FetchLatestStravaRunAsync(int userId, CancellationToken ct = default)
        {
            try
            {
                // Check if user is connected to Strava
                var user = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId, ct);

                if (user == null)
                {
                    return new FetchLatestRunResult
                    {
                        Success = false,
                        ErrorMessage = "User not found."
                    };
                }

                if (string.IsNullOrWhiteSpace(user.StravaAccessToken))
                {
                    return new FetchLatestRunResult
                    {
                        Success = false,
                        RequiresStravaConnection = true,
                        ErrorMessage = "Strava connection is required. Please connect your Strava account."
                    };
                }

                // Fetch latest run from Strava
                var activityJson = await _stravaApiService.GetLatestRunActivityAsync(user.StravaAccessToken);
                
                if (activityJson == null || !activityJson.HasValue)
                {
                    return new FetchLatestRunResult
                    {
                        Success = false,
                        ErrorMessage = "No run activities found or failed to fetch from Strava."
                    };
                }

                var activity = activityJson.Value;

                // Normalize Strava activity
                var normalized = StravaActivityNormalizer.Normalize(activity);
                if (normalized == null)
                {
                    return new FetchLatestRunResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid or incomplete activity data from Strava."
                    };
                }

                // Check if activity already exists
                var existingActivity = await _db.RunningActivities
                    .FirstOrDefaultAsync(
                        r => r.UserId == userId && r.ExternalActivityId == normalized.ExternalId,
                        ct);

                if (existingActivity != null)
                {
                    // Backfill route if missing (for activities synced before route feature was added)
                    if (existingActivity.Route == null && !string.IsNullOrWhiteSpace(normalized.SummaryPolyline))
                    {
                        SetRouteFromPolyline(existingActivity, normalized.SummaryPolyline);
                        existingActivity.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync(ct);
                    }
                    
                    return new FetchLatestRunResult
                    {
                        Success = true,
                        RunActivity = MapToDto(existingActivity)
                    };
                }

                // Create new RunningActivity from normalized data
                var now = DateTime.UtcNow;
                var runningActivity = CreateRunningActivityFromNormalized(userId, normalized, now);
                SetRouteFromPolyline(runningActivity, normalized.SummaryPolyline);

                _db.RunningActivities.Add(runningActivity);
                await _db.SaveChangesAsync(ct);

                // Update challenge progress and evaluate badges (non-blocking)
                try
                {
                    await _challengeProgressService.UpdateAfterRunSaved(runningActivity.Id, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update challenge progress for run {RunId}", runningActivity.Id);
                }

                try
                {
                    await _badgeEvaluationService.EvaluateBadgeConditionsAsync(userId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to evaluate badge conditions for user {UserId} after run {RunId}", userId, runningActivity.Id);
                    // Continue — badge evaluation must not break the run save flow.
                }

                return new FetchLatestRunResult
                {
                    Success = true,
                    RunActivity = MapToDto(runningActivity)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest Strava run for user {UserId}", userId);
                return new FetchLatestRunResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while fetching the latest run from Strava."
                };
            }
        }

        public async Task<IReadOnlyList<RunActivityDto>> GetRecentAsync(int userId, int limit, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken ct = default)
        {
            // Normalize limit
            if (limit < 1) limit = 100;
            if (limit > 1000) limit = 1000; // Safety limit

            // Check if user is connected to Strava
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null)
            {
                return new List<RunActivityDto>();
            }

            if (string.IsNullOrWhiteSpace(user.StravaAccessToken))
            {
                // If no Strava connection, return from database
                IQueryable<RunningActivity> baseQuery = _db.RunningActivities
                    .AsNoTracking()
                    .Where(r => r.UserId == userId);

                // Apply date filtering
                if (startDate.HasValue && endDate.HasValue)
                {
                    baseQuery = baseQuery.Where(r => r.RunDate >= startDate.Value && r.RunDate <= endDate.Value);
                }
                else if (startDate.HasValue)
                {
                    baseQuery = baseQuery.Where(r => r.RunDate >= startDate.Value);
                }
                else if (endDate.HasValue)
                {
                    baseQuery = baseQuery.Where(r => r.RunDate <= endDate.Value);
                }

                var activities = await baseQuery
                    .OrderByDescending(r => r.StartTime)
                    .Take(limit)
                    .ToListAsync(ct);

                return activities.Select(MapToDto).ToList();
            }

            // Fetch from Strava directly
            var stravaActivities = await _stravaApiService.GetAllRunActivitiesAsync(user.StravaAccessToken, limit);
            var now = DateTime.UtcNow;

            // Normalize all first so we can load existing records in one query
            var normalized = stravaActivities
                .Select(StravaActivityNormalizer.Normalize)
                .Where(n => n != null)
                .Select(n => n!)
                .ToList();

            var externalIds = normalized.Select(n => n.ExternalId).ToHashSet();
            var existingByExternalId = await _db.RunningActivities
                .Where(r => r.UserId == userId && externalIds.Contains(r.ExternalActivityId))
                .ToDictionaryAsync(r => r.ExternalActivityId, ct);

            var result = new List<RunActivityDto>(normalized.Count);
            var activitiesToSave = new List<RunningActivity>();

            foreach (var n in normalized)
            {
                if (existingByExternalId.TryGetValue(n.ExternalId, out var existing))
                {
                    if (existing.Route == null && !string.IsNullOrWhiteSpace(n.SummaryPolyline))
                    {
                        _logger.LogInformation("Backfilling route for activity {ExternalId} (DB Id: {Id})", n.ExternalId, existing.Id);
                        SetRouteFromPolyline(existing, n.SummaryPolyline);
                        existing.UpdatedAt = now;
                    }
                    else if (existing.Route == null)
                    {
                        _logger.LogDebug("Activity {ExternalId} has no route and Strava summary_polyline is empty", n.ExternalId);
                    }

                    result.Add(MapToDto(existing));
                }
                else
                {
                    var newActivity = CreateRunningActivityFromNormalized(userId, n, now);
                    SetRouteFromPolyline(newActivity, n.SummaryPolyline);
                    activitiesToSave.Add(newActivity);
                }
            }

            // Persist all changes (backfills + new) in one round-trip
            if (activitiesToSave.Count > 0 || _db.ChangeTracker.HasChanges())
            {
                if (activitiesToSave.Count > 0)
                    _db.RunningActivities.AddRange(activitiesToSave);

                await _db.SaveChangesAsync(ct);

                // Map newly saved activities to DTOs (IDs are now assigned)
                result.AddRange(activitiesToSave.Select(MapToDto));

                // Update challenge progress for each new activity (non-blocking)
                foreach (var savedActivity in activitiesToSave)
                {
                    try
                    {
                        await _challengeProgressService.UpdateAfterRunSaved(savedActivity.Id, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update challenge progress for run {RunId}", savedActivity.Id);
                    }
                }

                // Evaluate badges once after all new activities are saved (non-blocking)
                if (activitiesToSave.Count > 0)
                {
                    try
                    {
                        await _badgeEvaluationService.EvaluateBadgeConditionsAsync(userId, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to evaluate badge conditions for user {UserId} after batch run sync", userId);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a RunningActivity entity from normalized Strava activity data.
        /// </summary>
        private static RunningActivity CreateRunningActivityFromNormalized(
            int userId,
            StravaActivityNormalizer.NormalizedActivity normalized,
            DateTime now)
        {
            return new RunningActivity
            {
                UserId = userId,
                ExternalActivityId = normalized.ExternalId,
                Name = normalized.Name,
                Type = normalized.Type,
                StartTime = normalized.StartDateTime.UtcDateTime,
                RunDate = normalized.RunDate,
                DistanceMeters = normalized.DistanceMeters,
                MovingTimeSeconds = normalized.MovingTimeSeconds,
                ElapsedTimeSeconds = normalized.ElapsedTimeSeconds,
                TotalElevationGain = normalized.TotalElevationGain,
                AverageSpeed = normalized.AverageSpeed,
                AverageHeartRate = normalized.AverageHeartRate,
                BurnedCalories = normalized.BurnedCalories,
                Source = "STRAVA",
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        /// <summary>
        /// Decodes polyline and sets the route on the RunningActivity entity.
        /// Also extracts and sets bounding box and convex hull metadata for spatial operations.
        /// Handles errors gracefully - activity will be saved without route if decoding fails.
        /// </summary>
        private void SetRouteFromPolyline(RunningActivity activity, string? summaryPolyline)
        {
            if (string.IsNullOrWhiteSpace(summaryPolyline))
                return;

            try
            {
                var coordinates = _polylineDecoder.Decode(summaryPolyline);
                if (coordinates != null)
                {
                    activity.Route = _geometryConverter.ToLineString(coordinates);

                    // Extract bounding box and convex hull from the route
                    if (activity.Route != null)
                    {
                        // Extract bounding box
                        var bbox = _boundingBoxService.Extract(activity.Route);
                        
                        if (bbox != null)
                        {
                            // Validate bounding box values
                            if (bbox.MinLat <= bbox.MaxLat && bbox.MinLng <= bbox.MaxLng)
                            {
                                activity.MinLat = bbox.MinLat;
                                activity.MaxLat = bbox.MaxLat;
                                activity.MinLng = bbox.MinLng;
                                activity.MaxLng = bbox.MaxLng;
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Invalid bounding box for activity {ExternalId}: MinLat={MinLat}, MaxLat={MaxLat}, MinLng={MinLng}, MaxLng={MaxLng}",
                                    activity.ExternalActivityId, bbox.MinLat, bbox.MaxLat, bbox.MinLng, bbox.MaxLng);
                                // Leave bounding box as null for invalid values
                            }
                        }

                        // Extract convex hull
                        var convexHull = _convexHullService.Extract(activity.Route);
                        if (convexHull != null)
                        {
                            activity.ConvexHull = convexHull;
                        }
                        else
                        {
                            _logger.LogDebug(
                                "Could not extract convex hull for activity {ExternalId} (may have insufficient points or collinear points)",
                                activity.ExternalActivityId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decode polyline for activity {ExternalId}", activity.ExternalActivityId);
                // Continue without route - activity will be saved without route
            }
        }

        private static RunActivityDto MapToDto(RunningActivity entity)
        {
            return new RunActivityDto
            {
                Id = entity.Id,
                ExternalId = entity.ExternalActivityId,
                Name = entity.Name,
                Type = entity.Type,
                RunDate = entity.RunDate,
                StartTime = entity.StartTime,
                MovingTimeSeconds = entity.MovingTimeSeconds,
                ElapsedTimeSeconds = entity.ElapsedTimeSeconds,
                DistanceMeters = entity.DistanceMeters,
                TotalElevationGain = entity.TotalElevationGain,
                AverageSpeed = entity.AverageSpeed,
                AverageHeartRate = entity.AverageHeartRate,
                BurnedCalories = entity.BurnedCalories,
                Source = entity.Source
            };
        }
    }
}
