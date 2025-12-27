using System.Text.Json;
using GradProject.Application.Interfaces;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GradProject.Infrastructure.Services
{
    public class RunActivityService : IRunActivityService
    {
        private readonly AppDbContext _db;
        private readonly StravaApiService _stravaApiService;
        private readonly ILogger<RunActivityService> _logger;

        public RunActivityService(
            AppDbContext db,
            StravaApiService stravaApiService,
            ILogger<RunActivityService> logger)
        {
            _db = db;
            _stravaApiService = stravaApiService;
            _logger = logger;
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

                // Extract activity data
                if (!activity.TryGetProperty("id", out var idElement) ||
                    !activity.TryGetProperty("start_date", out var startDateElement) ||
                    !activity.TryGetProperty("moving_time", out var movingTimeElement) ||
                    !activity.TryGetProperty("distance", out var distanceElement))
                {
                    return new FetchLatestRunResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid activity data from Strava."
                    };
                }

                var externalId = idElement.GetInt64().ToString();
                var startDateStr = startDateElement.GetString();
                
                if (string.IsNullOrWhiteSpace(startDateStr) || 
                    !DateTimeOffset.TryParse(startDateStr, out var startDateTime))
                {
                    return new FetchLatestRunResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid start date from Strava activity."
                    };
                }

                var durationSeconds = movingTimeElement.GetInt32();
                var distanceMeters = (float)distanceElement.GetDouble();

                // Extract burned calories if available
                int? burnedCalories = null;
                if (activity.TryGetProperty("calories", out var caloriesElement) && 
                    caloriesElement.ValueKind == JsonValueKind.Number)
                {
                    burnedCalories = caloriesElement.GetInt32();
                }

                // Derive runDate from startDateTime (using the timezone of the startDateTime)
                var runDate = DateOnly.FromDateTime(startDateTime.Date);

                // Check if activity already exists
                var existingActivity = await _db.RunActivities
                    .FirstOrDefaultAsync(
                        r => r.UserId == userId && r.ExternalId == externalId,
                        ct);

                if (existingActivity != null)
                {
                    // Return existing activity
                    return new FetchLatestRunResult
                    {
                        Success = true,
                        RunActivity = new RunActivityDto
                        {
                            Id = existingActivity.Id,
                            ExternalId = existingActivity.ExternalId,
                            RunDate = existingActivity.RunDate,
                            StartDateTime = existingActivity.StartDateTime,
                            DurationSeconds = existingActivity.DurationSeconds,
                            DistanceMeters = existingActivity.DistanceMeters,
                            BurnedCalories = existingActivity.BurnedCalories,
                            Source = existingActivity.Source
                        }
                    };
                }

                // Create new RunActivity
                var runActivity = new RunActivity
                {
                    UserId = userId,
                    ExternalId = externalId,
                    RunDate = runDate,
                    StartDateTime = startDateTime,
                    DurationSeconds = durationSeconds,
                    DistanceMeters = distanceMeters,
                    BurnedCalories = burnedCalories,
                    Source = "STRAVA"
                };

                _db.RunActivities.Add(runActivity);
                await _db.SaveChangesAsync(ct);

                return new FetchLatestRunResult
                {
                    Success = true,
                    RunActivity = new RunActivityDto
                    {
                        Id = runActivity.Id,
                        ExternalId = runActivity.ExternalId,
                        RunDate = runActivity.RunDate,
                        StartDateTime = runActivity.StartDateTime,
                        DurationSeconds = runActivity.DurationSeconds,
                        DistanceMeters = runActivity.DistanceMeters,
                        BurnedCalories = runActivity.BurnedCalories,
                        Source = runActivity.Source
                    }
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
    }
}

