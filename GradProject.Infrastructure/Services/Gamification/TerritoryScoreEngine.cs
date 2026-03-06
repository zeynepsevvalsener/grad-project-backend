using GradProject.Application.Interfaces.Gamification;
using GradProject.Application.Models;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Gamification;

/// <summary>
/// Stub implementation of territory score engine (HLN-8).
/// Returns one contribution per territory with a placeholder score so claim/defend flow works.
/// Replace with full formula from HLN-8 Territory Scoring Model when available.
/// </summary>
public class TerritoryScoreEngine : ITerritoryScoreEngine
{
    private readonly AppDbContext _db;

    public TerritoryScoreEngine(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TerritoryContribution>> ComputeRunContributionsAsync(
        int runId,
        IReadOnlyList<int> territoryIds,
        CancellationToken ct = default)
    {
        if (territoryIds.Count == 0)
            return Array.Empty<TerritoryContribution>();

        var run = await _db.RunningActivities
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run == null)
            return Array.Empty<TerritoryContribution>();

        var territoryIdSet = territoryIds.Distinct().ToHashSet();
        var existingTerritories = await _db.Territories
            .AsNoTracking()
            .Where(t => territoryIdSet.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync(ct);

        var paceSecPerKm = run.DistanceMeters > 0
            ? run.MovingTimeSeconds / (run.DistanceMeters / 1000.0)
            : 0.0;
        var coverageRatio = 0.5;
        var distanceInTerritory = run.DistanceMeters > 0
            ? run.DistanceMeters * coverageRatio / Math.Max(1, territoryIdSet.Count)
            : 0;

        return existingTerritories
            .Select(id => new TerritoryContribution
            {
                TerritoryId = id,
                FinalScore = Math.Round(0.3 + (distanceInTerritory / 10000.0) + (paceSecPerKm > 0 ? Math.Max(0, (600 - paceSecPerKm) / 600.0) * 0.2 : 0), 4),
                CoverageRatio = coverageRatio,
                DistanceInTerritory = distanceInTerritory
            })
            .ToList();
    }
}
