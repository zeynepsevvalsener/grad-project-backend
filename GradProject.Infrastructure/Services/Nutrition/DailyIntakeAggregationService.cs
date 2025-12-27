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

        public async Task<DailySummaryDto?> GetDailySummaryAsync(int userId, DateOnly date, CancellationToken ct = default)
        {
            var dailySummary = await _db.DailySummaries
                .AsNoTracking()
                .FirstOrDefaultAsync(ds => ds.UserId == userId && ds.Date == date, ct);

            if (dailySummary == null)
                return null;

            return new DailySummaryDto
            {
                Id = dailySummary.Id,
                Date = dailySummary.Date,
                TotalIntakeCalories = dailySummary.TotalIntakeCalories,
                TotalProtein = dailySummary.TotalProtein,
                TotalCarbs = dailySummary.TotalCarbs,
                TotalFat = dailySummary.TotalFat
            };
        }
    }
}

