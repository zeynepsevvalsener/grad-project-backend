using GradProject.Application.DTOs.Leaderboard;

namespace GradProject.Application.Services.Leaderboard;

/// <summary>
/// Deterministic multi-criteria ranking aligned with SoW (HLN-8).
/// <para>
/// Fixed sort order (all leaderboards):
///   1. Territory score DESC (null → 0)
///   2. Total distance DESC
///   3. Pace ASC (seconds per km — lower is better; no distance → double.MaxValue)
///   4. Completion speed ASC (seconds from challenge start to finish — lower is better; null → long.MaxValue)
///   5. UserId ASC (deterministic tie-breaker)
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
            .OrderByDescending(x => x.TerritoryScore ?? 0)
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
            TotalDistance = (long)entry.TotalDistance,
            AveragePace = Math.Round(entry.AveragePace, 2),
            CompletionSpeed = entry.CompletionSpeed
        }).ToList();
    }
}
