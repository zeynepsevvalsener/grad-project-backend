using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Gamification
{
    public class TerritoryAchievementService
    {
        private readonly AppDbContext _db;

        public TerritoryAchievementService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Checks and awards territory-based achievements for a user after a run in a territory.
        /// </summary>
        public async Task AwardTerritoryAchievementsAsync(int userId, int territoryId, int runningActivityId, CancellationToken ct = default)
        {
            // Get all runs in this territory
            var runsInTerritory = await _db.RunningActivities
                .Where(r => r.UserId == userId && r.Route != null /* TODO: Add territory check logic */)
                .ToListAsync(ct);

            var currentRun = runsInTerritory.FirstOrDefault(r => r.Id == runningActivityId);
            if (currentRun == null)
                return;

            // 1. Longest duration
            var longestRun = runsInTerritory.OrderByDescending(r => r.MovingTimeSeconds).FirstOrDefault();
            if (longestRun != null && longestRun.Id == currentRun.Id)
            {
                // Award "Longest Duration in Territory" achievement
                await AwardBadgeAsync(userId, territoryId, "LongestDuration", ct);
            }

            // 2. Most distance
            var longestDistanceRun = runsInTerritory.OrderByDescending(r => r.DistanceMeters).FirstOrDefault();
            if (longestDistanceRun != null && longestDistanceRun.Id == currentRun.Id)
            {
                // Award "Most Distance in Territory" achievement
                await AwardBadgeAsync(userId, territoryId, "MostDistance", ct);
            }

            // 3. Fastest run (lowest average pace, but only for runs > 1km)
            var fastestRun = runsInTerritory
                .Where(r => r.DistanceMeters > 1000)
                .OrderBy(r => r.MovingTimeSeconds / r.DistanceMeters)
                .FirstOrDefault();
            if (fastestRun != null && fastestRun.Id == currentRun.Id)
            {
                // Award "Fastest Run in Territory" achievement
                await AwardBadgeAsync(userId, territoryId, "FastestRun", ct);
            }
        }

        private async Task AwardBadgeAsync(int userId, int territoryId, string achievementType, CancellationToken ct)
        {
            // TODO: Implement badge creation/assignment logic, e.g. create UserBadge or similar entity
            // This is a placeholder for actual badge/achievement logic
        }
    }
}
