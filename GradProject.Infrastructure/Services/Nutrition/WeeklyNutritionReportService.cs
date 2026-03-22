using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class WeeklyNutritionReportService : IWeeklyNutritionReportService
    {
        private readonly AppDbContext _db;

        public WeeklyNutritionReportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<WeeklyNutritionReportDto> GetWeeklyReportAsync(
            int userId,
            DateOnly weekStart,
            CancellationToken ct = default)
        {
            var weekEnd = weekStart.AddDays(6);

            var summaries = await _db.DailySummaries
                .AsNoTracking()
                .Where(ds =>
                    ds.UserId == userId &&
                    ds.Date >= weekStart &&
                    ds.Date <= weekEnd &&
                    ds.TotalIntakeCalories > 0)
                .OrderBy(ds => ds.Date)
                .ToListAsync(ct);

            var days = summaries
                .Select(ds =>
                {
                    var calorieScore = ScoreMetric(ds.TotalIntakeCalories, ds.CalorieTarget);
                    var proteinScore = ScoreMetric((double)ds.TotalProtein, (double?)ds.ProteinTargetG);
                    var carbScore = ScoreMetric((double)ds.TotalCarbs, (double?)ds.CarbTargetG);
                    var fatScore = ScoreMetric((double)ds.TotalFat, (double?)ds.FatTargetG);

                    var dailyScore = (calorieScore * 0.60m)
                                   + (proteinScore * 0.133m)
                                   + (carbScore * 0.133m)
                                   + (fatScore * 0.133m);

                    return new DailyNutritionScoreDto
                    {
                        Date = ds.Date,
                        TotalIntakeCalories = ds.TotalIntakeCalories,
                        TotalProtein = ds.TotalProtein,
                        TotalCarbs = ds.TotalCarbs,
                        TotalFat = ds.TotalFat,
                        CalorieTarget = ds.CalorieTarget,
                        ProteinTargetG = ds.ProteinTargetG,
                        CarbTargetG = ds.CarbTargetG,
                        FatTargetG = ds.FatTargetG,
                        DailyScore = Math.Round(dailyScore, 1),
                        CalorieScore = calorieScore,
                        ProteinScore = proteinScore,
                        CarbScore = carbScore,
                        FatScore = fatScore
                    };
                })
                .ToList();

            var averageScore = days.Count > 0
                ? Math.Round(days.Average(d => d.DailyScore), 1)
                : 0m;

            return new WeeklyNutritionReportDto
            {
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                TrackedDays = days.Count,
                AverageScore = averageScore,
                Days = days
            };
        }

        // Hedefe yakınlık puanı (0-10)
        private static decimal ScoreMetric(double actual, double? target)
        {
            // Hedef yoksa bu metriği atlıyoruz — tam puan ver
            if (target is null or 0)
                return 10m;

            var deviation = Math.Abs(actual - target.Value) / target.Value;

            return deviation switch
            {
                <= 0.05 => 10m,
                <= 0.10 => 8m,
                <= 0.20 => 6m,
                <= 0.30 => 4m,
                <= 0.50 => 2m,
                _ => 0m
            };
        }

        private static decimal ScoreMetric(int actual, int? target)
            => ScoreMetric((double)actual, (double?)target);
    }
}