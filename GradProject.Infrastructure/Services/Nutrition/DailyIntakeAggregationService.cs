using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class DailyIntakeAggregationService : IDailyIntakeAggregationService
    {
        private readonly AppDbContext _db;
        private readonly IChallengeProgressService _challengeProgressService;
        private readonly ILogger<DailyIntakeAggregationService> _logger;

        public DailyIntakeAggregationService(
            AppDbContext db,
            IChallengeProgressService challengeProgressService,
            ILogger<DailyIntakeAggregationService> logger)
        {
            _db = db;
            _challengeProgressService = challengeProgressService;
            _logger = logger;
        }

        public async Task AggregateDailyIntakeAsync(int userId, DateOnly date, CancellationToken ct = default)
        {
            var start = date.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);

            // ✅ Source of truth: ConsumedFoods
            var consumedFoods = await _db.ConsumedFoods
                .Include(cf => cf.Food)
                .Where(cf => cf.UserId == userId && cf.ConsumedAt >= start && cf.ConsumedAt < end)
                .ToListAsync(ct);

            var totalCalories = 0;
            var totalProtein = 0m;
            var totalCarbs = 0m;
            var totalFat = 0m;

            foreach (var consumedFood in consumedFoods)
            {
                var multiplier = consumedFood.PortionG / 100m;
                totalCalories += (int)Math.Round(consumedFood.Food.Kcal * multiplier);
                totalProtein += consumedFood.Food.ProteinG * multiplier;
                totalCarbs += consumedFood.Food.CarbG * multiplier;
                totalFat += consumedFood.Food.FatG * multiplier;
            }

            // Burned calories (run)
            var latestRun = await _db.RunningActivities
                .Where(r => r.UserId == userId && r.RunDate == date)
                .OrderByDescending(r => r.StartTime)
                .FirstOrDefaultAsync(ct);

            if (latestRun != null)
            {
                var calculatedCalories = await CalculateBurnedCaloriesAsync(latestRun.DistanceMeters, latestRun.MovingTimeSeconds, userId, ct);
                if (calculatedCalories.HasValue)
                {
                    latestRun.BurnedCalories = calculatedCalories.Value;
                }
            }

            var dailySummary = await _db.DailySummaries
                .FirstOrDefaultAsync(ds => ds.UserId == userId && ds.Date == date, ct);

            var previousCalories = dailySummary?.TotalIntakeCalories ?? 0;
            var caloriesDifference = totalCalories - previousCalories;

            if (dailySummary == null)
            {
                dailySummary = new Domain.Entities.DailySummary
                {
                    UserId = userId,
                    Date = date,
                    TotalIntakeCalories = totalCalories,
                    TotalProtein = totalProtein,
                    TotalCarbs = totalCarbs,
                    TotalFat = totalFat
                };
                _db.DailySummaries.Add(dailySummary);
            }
            else
            {
                dailySummary.TotalIntakeCalories = totalCalories;
                dailySummary.TotalProtein = totalProtein;
                dailySummary.TotalCarbs = totalCarbs;
                dailySummary.TotalFat = totalFat;
            }

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Aggregating daily intake for user {UserId} on date {Date}: TotalCalories={TotalCalories}, ConsumedFoods={ConsumedFoodsCount}",
                userId, date, totalCalories, consumedFoods.Count);

            if (totalCalories > 0)
            {
                try
                {
                    _logger.LogInformation(
                        "Updating challenge progress for user {UserId} on date {Date} with {TotalCalories} calories",
                        userId, date, totalCalories);

                    await _challengeProgressService.UpdateAfterNutritionSaved(userId, totalCalories, date, ct);

                    _logger.LogInformation(
                        "Challenge progress updated successfully for user {UserId} on date {Date}",
                        userId, date);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to update challenge progress for nutrition data for user {UserId} on date {Date}",
                        userId, date);
                }
            }
            else
            {
                _logger.LogDebug("No calories to update challenge progress for user {UserId} on date {Date}", userId, date);
            }
        }

        private async Task<int?> CalculateBurnedCaloriesAsync(double distanceMeters, int durationSeconds, int userId, CancellationToken ct)
        {
            if (distanceMeters <= 0 || durationSeconds <= 0)
                return null;

            var profile = await _db.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile?.Weight == null || profile.Weight <= 0)
                return null;

            var weightKg = (double)profile.Weight.Value;
            var distanceKm = distanceMeters / 1000.0;
            var durationHours = durationSeconds / 3600.0;

            var speedKmh = durationHours > 0 ? distanceKm / durationHours : 0;

            var met = CalculateMetValue(speedKmh);

            var burnedCalories = met * weightKg * durationHours;

            return (int)Math.Round(burnedCalories);
        }

        private double CalculateMetValue(double speedKmh)
        {
            if (speedKmh < 6.5)
                return 6.0;
            else if (speedKmh < 8.0)
                return 7.0;
            else if (speedKmh < 9.7)
                return 8.0;
            else if (speedKmh < 11.3)
                return 9.0;
            else
                return 10.0;
        }

        public async Task<DailySummaryDto?> GetDailySummaryAsync(int userId, DateOnly date, CancellationToken ct = default)
        {
            var dailySummary = await _db.DailySummaries
                .AsNoTracking()
                .FirstOrDefaultAsync(ds => ds.UserId == userId && ds.Date == date, ct);

            if (dailySummary == null)
                return null;

            var latestRun = await _db.RunningActivities
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.RunDate == date)
                .OrderByDescending(r => r.StartTime)
                .FirstOrDefaultAsync(ct);

            int? burnedCalories = null;
            if (latestRun != null)
            {
                if (latestRun.BurnedCalories.HasValue)
                {
                    burnedCalories = latestRun.BurnedCalories.Value;
                }
                else
                {
                    burnedCalories = await CalculateBurnedCaloriesAsync(latestRun.DistanceMeters, latestRun.MovingTimeSeconds, userId, ct);
                }
            }

            return new DailySummaryDto
            {
                Id = dailySummary.Id,
                Date = dailySummary.Date,
                TotalIntakeCalories = dailySummary.TotalIntakeCalories,
                TotalProtein = dailySummary.TotalProtein,
                TotalCarbs = dailySummary.TotalCarbs,
                TotalFat = dailySummary.TotalFat,
                BurnedCalories = burnedCalories
            };
        }
    }
}