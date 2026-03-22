using GradProject.Application.DTOs.Leaderboard;

namespace GradProject.Application.Services.Leaderboard;

/// <summary>
/// Deterministic multi-criteria ranking aligned with SoW (HLN-8).
/// <para>
/// Fixed sort order (all leaderboards):
///   1. Territory count DESC (more owned territories rank higher)
///   2. Territory score DESC (null → 0)
///   3. Total distance DESC
///   4. Pace ASC (seconds per km — lower is better; no distance → double.MaxValue)
///   5. Completion speed ASC (seconds from challenge start to finish — lower is better; null → long.MaxValue)
///   6. UserId ASC (deterministic tie-breaker)
/// </para>
/// <para>Standard 1-based rank; no dense ranking.</para>
/// </summary>
public class RankingEngine
{
    /// <summary>
    /// Ranks the aggregated data using the fixed SoW sort order.
    /// </summary>
    public List<LeaderboardEntryDto> CalculateRanks(List<LeaderboardAggregateData> aggregatedData)
    {
        var sorted = aggregatedData
            .OrderByDescending(x => x.TerritoryCount)
            .ThenByDescending(x => x.TerritoryScore ?? 0)
            .ThenByDescending(x => x.TotalDistance)
            .ThenBy(x => x.AveragePace)
            .ThenBy(x => x.CompletionSpeed ?? long.MaxValue)
            .ThenBy(x => x.UserId)
            .ToList();

        return sorted.Select((entry, index) => new LeaderboardEntryDto
        {
            Rank = index + 1,
            UserId = entry.UserId,
            Username = entry.Username,
            TerritoryScore = entry.TerritoryScore,
            TerritoryCount = entry.TerritoryCount,
            TotalDistance = (long)entry.TotalDistance,
            AveragePace = Math.Round(entry.AveragePace, 2),
            CompletionSpeed = entry.CompletionSpeed
        }).ToList();
    }
}
