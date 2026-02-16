using GradProject.Application.DTOs.Common;
using GradProject.Application.Interfaces;
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

        public RunActivityService(
            AppDbContext db,
            StravaApiService stravaApiService,
            ILogger<RunActivityService> logger,
            PolylineDecoder polylineDecoder,
            GeometryConverter geometryConverter)
        {
            _db = db;
            _stravaApiService = stravaApiService;
            _logger = logger;
            _polylineDecoder = polylineDecoder;
            _geometryConverter = geometryConverter;
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
            
            var result = new List<RunActivityDto>();
            var activitiesToSave = new List<RunningActivity>();
            var now = DateTime.UtcNow;

            foreach (var activityJson in stravaActivities)
            {
                var normalized = StravaActivityNormalizer.Normalize(activityJson);
                if (normalized == null)
                    continue;

                // Check if exists in DB
                var existing = await _db.RunningActivities
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.ExternalActivityId == normalized.ExternalId, ct);

                if (existing != null)
                {
                    // Backfill route if missing (for activities synced before route feature was added)
                    if (existing.Route == null && !string.IsNullOrWhiteSpace(normalized.SummaryPolyline))
                    {
                        _logger.LogInformation("Backfilling route for activity {ExternalId} (DB Id: {Id})", normalized.ExternalId, existing.Id);
                        SetRouteFromPolyline(existing, normalized.SummaryPolyline);
                        existing.UpdatedAt = now;
                        await _db.SaveChangesAsync(ct);
                        _logger.LogInformation("Route backfilled successfully for activity {ExternalId}", normalized.ExternalId);
                    }
                    else if (existing.Route == null)
                    {
                        _logger.LogDebug("Activity {ExternalId} has no route and Strava summary_polyline is empty", normalized.ExternalId);
                    }
                    
                    result.Add(MapToDto(existing));
                }
                else
                {
                    // Create new activity to save
                    var newActivity = CreateRunningActivityFromNormalized(userId, normalized, now);
                    SetRouteFromPolyline(newActivity, normalized.SummaryPolyline);

                    activitiesToSave.Add(newActivity);
                    result.Add(MapToDto(newActivity));
                }
            }

            // Save new activities to database in batch
            if (activitiesToSave.Count > 0)
            {
                _db.RunningActivities.AddRange(activitiesToSave);
                await _db.SaveChangesAsync(ct);
                
                // Update IDs in result for newly saved activities
                for (int i = 0; i < activitiesToSave.Count; i++)
                {
                    var savedActivity = activitiesToSave[i];
                    var dtoIndex = result.FindIndex(r => r.ExternalId == savedActivity.ExternalActivityId && r.Id == 0);
                    if (dtoIndex >= 0)
                    {
                        result[dtoIndex] = MapToDto(savedActivity);
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
