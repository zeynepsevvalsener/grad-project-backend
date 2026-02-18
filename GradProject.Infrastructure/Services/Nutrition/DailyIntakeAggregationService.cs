using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Domain.Entities;
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
            
            // Get ConsumedFoods for the date
            var consumedFoods = await _db.ConsumedFoods
                .Include(cf => cf.Food)
                .Where(cf => cf.UserId == userId && cf.ConsumedAt >= start && cf.ConsumedAt < end)
                .ToListAsync(ct);

            // Get Meals with MealFoods for the date
            var meals = await _db.Meals
                .Include(m => m.MealFoods)
                    .ThenInclude(mf => mf.Food)
                .Where(m => m.UserId == userId && m.LoggedAt >= start && m.LoggedAt < end)
                .ToListAsync(ct);

            var totalCalories = 0;
            var totalProtein = 0m;
            var totalCarbs = 0m;
            var totalFat = 0m;

            // Calculate from ConsumedFoods
            foreach (var consumedFood in consumedFoods)
            {
                var multiplier = consumedFood.PortionG / 100m;
                totalCalories += (int)Math.Round(consumedFood.Food.Kcal * multiplier);
                totalProtein += consumedFood.Food.ProteinG * multiplier;
                totalCarbs += consumedFood.Food.CarbG * multiplier;
                totalFat += consumedFood.Food.FatG * multiplier;
            }

            // Calculate from MealFoods (Meals)
            foreach (var meal in meals)
            {
                foreach (var mealFood in meal.MealFoods)
                {
                    // Convert quantity to grams based on unit
                    decimal portionG = mealFood.Unit.ToLower() switch
                    {
                        "g" or "gram" or "grams" => mealFood.Quantity,
                        "kg" or "kilogram" or "kilograms" => mealFood.Quantity * 1000m,
                        "oz" or "ounce" or "ounces" => mealFood.Quantity * 28.35m,
                        "lb" or "pound" or "pounds" => mealFood.Quantity * 453.592m,
                        _ => mealFood.Quantity // Default: assume grams
                    };

                    var multiplier = portionG / 100m;
                    totalCalories += (int)Math.Round(mealFood.Food.Kcal * multiplier);
                    totalProtein += mealFood.Food.ProteinG * multiplier;
                    totalCarbs += mealFood.Food.CarbG * multiplier;
                    totalFat += mealFood.Food.FatG * multiplier;
                }
            }

            // Get latest run for the date to calculate burned calories
            var latestRun = await _db.RunningActivities
                .Where(r => r.UserId == userId && r.RunDate == date)
                .OrderByDescending(r => r.StartTime)
                .FirstOrDefaultAsync(ct);

            if (latestRun != null)
            {
                // Always calculate burned calories using our formula (ignore Strava's value)
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
                dailySummary = new DailySummary
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

            // Update challenge progress with total calories for the day (non-blocking)
            // The service will handle incremental updates by tracking which calories have already been counted
            _logger.LogInformation("Aggregating daily intake for user {UserId} on date {Date}: TotalCalories={TotalCalories}, ConsumedFoods={ConsumedFoodsCount}, Meals={MealsCount}", 
                userId, date, totalCalories, consumedFoods.Count, meals.Count);
            
            if (totalCalories > 0)
            {
                try
                {
                    _logger.LogInformation("Updating challenge progress for user {UserId} on date {Date} with {TotalCalories} calories", userId, date, totalCalories);
                    await _challengeProgressService.UpdateAfterNutritionSaved(userId, totalCalories, date, ct);
                    _logger.LogInformation("Challenge progress updated successfully for user {UserId} on date {Date}", userId, date);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update challenge progress for nutrition data for user {UserId} on date {Date}", userId, date);
                    // Continue - don't break aggregation flow
                }
            }
            else
            {
                _logger.LogDebug("No calories to update challenge progress for user {UserId} on date {Date}", userId, date);
            }
        }

        private async Task<int?> CalculateBurnedCaloriesAsync(double distanceMeters, int durationSeconds, int userId, CancellationToken ct)
        {
            // Validate inputs
            if (distanceMeters <= 0 || durationSeconds <= 0)
                return null;

            // Get user profile for weight, height, gender
            var profile = await _db.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile?.Weight == null || profile.Weight <= 0)
                return null;

            var weightKg = (double)profile.Weight.Value;
            var distanceKm = distanceMeters / 1000.0;
            var durationHours = durationSeconds / 3600.0;

            // Calculate average speed (km/h)
            var speedKmh = durationHours > 0 ? distanceKm / durationHours : 0;

            // MET (Metabolic Equivalent) value based on running speed
            // Running MET values: 6 (jogging ~6 km/h) to 10 (fast running ~10+ km/h)
            var met = CalculateMetValue(speedKmh);

            // Formula: Calories = MET × Weight (kg) × Time (hours)
            var burnedCalories = met * weightKg * durationHours;

            return (int)Math.Round(burnedCalories);
        }

        private double CalculateMetValue(double speedKmh)
        {
            // MET values for running based on speed (km/h)
            // Source: Compendium of Physical Activities
            if (speedKmh < 6.5)
                return 6.0;  // Jogging
            else if (speedKmh < 8.0)
                return 7.0;  // Running, 6-7 km/h
            else if (speedKmh < 9.7)
                return 8.0;  // Running, 8 km/h
            else if (speedKmh < 11.3)
                return 9.0;  // Running, 9 km/h
            else
                return 10.0; // Running, 10+ km/h (fast)
        }

        public async Task<DailySummaryDto?> GetDailySummaryAsync(int userId, DateOnly date, CancellationToken ct = default)
        {
            var dailySummary = await _db.DailySummaries
                .AsNoTracking()
                .FirstOrDefaultAsync(ds => ds.UserId == userId && ds.Date == date, ct);

            if (dailySummary == null)
                return null;

            // Get burned calories from the latest run for this date
            var latestRun = await _db.RunningActivities
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.RunDate == date)
                .OrderByDescending(r => r.StartTime)
                .FirstOrDefaultAsync(ct);

            int? burnedCalories = null;
            if (latestRun != null)
            {
                // If BurnedCalories is already calculated, use it
                // Otherwise, calculate it using our formula (profile: gender, height, weight + run: distance, duration)
                if (latestRun.BurnedCalories.HasValue)
                {
                    burnedCalories = latestRun.BurnedCalories.Value;
                }
                else
                {
                    // Calculate burned calories using profile (gender, height, weight) and run (distance, duration)
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

