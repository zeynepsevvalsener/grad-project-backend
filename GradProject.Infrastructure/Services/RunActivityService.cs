using GradProject.Application.Interfaces;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
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

                // Normalize Strava activity (date/time, distance, duration, calories with fallbacks and cleanup)
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
                var existingActivity = await _db.RunActivities
                    .FirstOrDefaultAsync(
                        r => r.UserId == userId && r.ExternalId == normalized.ExternalId,
                        ct);

                if (existingActivity != null)
                {
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

                // Create new RunActivity from normalized data
                var runActivity = new RunActivity
                {
                    UserId = userId,
                    ExternalId = normalized.ExternalId,
                    RunDate = normalized.RunDate,
                    StartDateTime = normalized.StartDateTime,
                    DurationSeconds = normalized.DurationSeconds,
                    DistanceMeters = normalized.DistanceMeters,
                    BurnedCalories = normalized.BurnedCalories,
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

