using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class DailyIntakeAggregationService : IDailyIntakeAggregationService
    {
        private readonly AppDbContext _db;

        public DailyIntakeAggregationService(AppDbContext db)
        {
            _db = db;
        }

        public async Task AggregateDailyIntakeAsync(int userId, DateOnly date, CancellationToken ct = default)
        {

            var start = date.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);
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

