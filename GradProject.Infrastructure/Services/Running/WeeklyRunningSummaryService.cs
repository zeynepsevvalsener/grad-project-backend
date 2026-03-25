using System.Globalization;
using GradProject.Application.DTOs.Running;
using GradProject.Application.Interfaces.Running;
using GradProject.Application.Utilities;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Running
{
    public class WeeklyRunningSummaryService : IWeeklyRunningSummaryService
    {
        private readonly AppDbContext _db;

        public WeeklyRunningSummaryService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<WeeklyRunningSummaryDto> GetWeeklySummaryAsync(
            int userId,
            int? isoYear,
            int? isoWeek,
            bool includeWeekOverWeek = true,
            CancellationToken ct = default)
        {
            var (year, week) = ResolveIsoYearWeek(isoYear, isoWeek);
            var (weekStart, weekEnd) = GetIsoWeekRange(year, week);

            var runs = await _db.RunningActivities
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.RunDate >= weekStart && r.RunDate <= weekEnd)
                .OrderBy(r => r.RunDate)
                .ThenBy(r => r.StartTime)
                .ToListAsync(ct);

            WeeklyRunningWeekOverWeekDto? wow = null;
            if (includeWeekOverWeek)
            {
                var prevMonday = weekStart.AddDays(-7);
                var prevDt = prevMonday.ToDateTime(TimeOnly.MinValue);
                var prevYear = ISOWeek.GetYear(prevDt);
                var prevWeek = ISOWeek.GetWeekOfYear(prevDt);
                var (prevStart, prevEnd) = GetIsoWeekRange(prevYear, prevWeek);

                var prevRuns = await _db.RunningActivities
                    .AsNoTracking()
                    .Where(r => r.UserId == userId && r.RunDate >= prevStart && r.RunDate <= prevEnd)
                    .ToListAsync(ct);

                var prevDistance = prevRuns.Sum(r => r.DistanceMeters);
                var currDistance = runs.Sum(r => r.DistanceMeters);
                double? pct = null;
                if (prevDistance > 0)
                {
                    pct = Math.Round((currDistance - prevDistance) / prevDistance * 100.0, 2, MidpointRounding.AwayFromZero);
                }

                wow = new WeeklyRunningWeekOverWeekDto
                {
                    PreviousIsoYear = prevYear,
                    PreviousIsoWeek = prevWeek,
                    PreviousWeekStart = prevStart,
                    PreviousWeekEnd = prevEnd,
                    PreviousTotalDistanceMeters = Math.Round(prevDistance, 2, MidpointRounding.AwayFromZero),
                    PreviousRunCount = prevRuns.Count,
                    DistanceChangePercent = pct
                };
            }

            return BuildDto(year, week, weekStart, weekEnd, runs, wow);
        }

        private static (int year, int week) ResolveIsoYearWeek(int? isoYear, int? isoWeek)
        {
            if (isoYear == null && isoWeek == null)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var dt = today.ToDateTime(TimeOnly.MinValue);
                return (ISOWeek.GetYear(dt), ISOWeek.GetWeekOfYear(dt));
            }

            if (isoYear == null || isoWeek == null)
                throw new ArgumentException("Both isoYear and isoWeek must be provided together, or omit both for the current week.");

            if (isoWeek < 1 || isoWeek > 53)
                throw new ArgumentOutOfRangeException(nameof(isoWeek), "ISO week must be between 1 and 53.");

            var y = isoYear.Value;
            var w = isoWeek.Value;
            var weeksInYear = ISOWeek.GetWeeksInYear(y);
            if (w > weeksInYear)
                throw new ArgumentOutOfRangeException(nameof(isoWeek), $"ISO week {w} is invalid for year {y} (that year has {weeksInYear} weeks).");

            return (y, w);
        }

        private static (DateOnly WeekStart, DateOnly WeekEnd) GetIsoWeekRange(int isoYear, int isoWeek)
        {
            var monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday));
            var sunday = monday.AddDays(6);
            return (monday, sunday);
        }

        private static WeeklyRunningSummaryDto BuildDto(
            int isoYear,
            int isoWeek,
            DateOnly weekStart,
            DateOnly weekEnd,
            List<RunningActivity> runs,
            WeeklyRunningWeekOverWeekDto? wow)
        {
            var totalDistance = runs.Sum(r => r.DistanceMeters);
            var totalMoving = runs.Sum(r => r.MovingTimeSeconds);
            var totalElapsed = runs.Sum(r => r.ElapsedTimeSeconds);
            var totalElev = runs.Sum(r => r.TotalElevationGain);
            var calories = runs.Where(r => r.BurnedCalories.HasValue).Sum(r => r.BurnedCalories!.Value);
            var hasCalories = runs.Any(r => r.BurnedCalories.HasValue);

            var hrRuns = runs.Where(r => r.AverageHeartRate.HasValue).ToList();
            double? avgHr = hrRuns.Count > 0
                ? Math.Round(hrRuns.Average(r => r.AverageHeartRate!.Value), 2, MidpointRounding.AwayFromZero)
                : null;

            var pace = RunningPaceCalculator.FromDistanceAndTime(totalDistance, totalMoving);

            var longest = runs.Count > 0 ? runs.Max(r => r.DistanceMeters) : 0.0;

            var daily = runs
                .GroupBy(r => r.RunDate)
                .OrderBy(g => g.Key)
                .Select(g => new DailyRunningStatDto
                {
                    Date = g.Key,
                    RunCount = g.Count(),
                    DistanceMeters = Math.Round(g.Sum(x => x.DistanceMeters), 2, MidpointRounding.AwayFromZero),
                    MovingTimeSeconds = g.Sum(x => x.MovingTimeSeconds)
                })
                .ToList();

            return new WeeklyRunningSummaryDto
            {
                IsoYear = isoYear,
                IsoWeek = isoWeek,
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                RunCount = runs.Count,
                ActiveDays = daily.Count,
                TotalDistanceMeters = Math.Round(totalDistance, 2, MidpointRounding.AwayFromZero),
                TotalMovingTimeSeconds = totalMoving,
                TotalElapsedTimeSeconds = totalElapsed,
                TotalElevationGain = Math.Round(totalElev, 2, MidpointRounding.AwayFromZero),
                TotalCaloriesBurned = hasCalories ? calories : null,
                AverageHeartRate = avgHr,
                AveragePaceMinutesPerKm = pace,
                LongestSingleRunDistanceMeters = Math.Round(longest, 2, MidpointRounding.AwayFromZero),
                DailyBreakdown = daily,
                VsPreviousIsoWeek = wow
            };
        }

    }
}
