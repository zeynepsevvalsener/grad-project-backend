using GradProject.Application.DTOs.Leaderboard;
using GradProject.Domain.Enums;

namespace GradProject.Application.Services.Leaderboard;

/// <summary>
/// Deterministic multi-criteria ranking. Sort order varies by ChallengeMetric; UserId tie-breaker.
/// </summary>
public class RankingEngine
{
    /// <summary>
    /// Calculates ranks. Primary sort by metric: Distance→TotalDistance, Pace→AveragePace, Duration→CompletionSpeed; else Territory+Distance+Pace+Completion (karma).
    /// </summary>
    public List<LeaderboardEntryDto> CalculateRanks(
        List<LeaderboardAggregateData> aggregatedData,
        ChallengeMetric? metric = null)
    {
        var ordered = metric switch
        {
            ChallengeMetric.Distance => aggregatedData
                .OrderByDescending(x => x.TotalDistance)
                .ThenByDescending(x => x.TerritoryScore ?? 0)
                .ThenBy(x => x.AveragePace)
                .ThenBy(x => x.CompletionSpeed ?? long.MaxValue)
                .ThenBy(x => x.UserId),
            ChallengeMetric.Pace => aggregatedData
                .OrderBy(x => x.AveragePace)
                .ThenByDescending(x => x.TotalDistance)
                .ThenByDescending(x => x.TerritoryScore ?? 0)
                .ThenBy(x => x.CompletionSpeed ?? long.MaxValue)
                .ThenBy(x => x.UserId),
            ChallengeMetric.Duration => aggregatedData
                .OrderBy(x => x.CompletionSpeed ?? long.MaxValue)
                .ThenByDescending(x => x.TotalDistance)
                .ThenByDescending(x => x.TerritoryScore ?? 0)
                .ThenBy(x => x.AveragePace)
                .ThenBy(x => x.UserId),
            _ => aggregatedData
                .OrderByDescending(x => x.TerritoryScore ?? 0)
                .ThenByDescending(x => x.TotalDistance)
                .ThenBy(x => x.AveragePace)
                .ThenBy(x => x.CompletionSpeed ?? long.MaxValue)
                .ThenBy(x => x.UserId)
        };

        var sorted = ordered.ToList();
        // Map aggregated data to response DTOs with formatted values
        var ranked = sorted.Select((entry, index) => new LeaderboardEntryDto
        {
            Rank = index + 1,  // 1-based ranking (first place = 1)
            UserId = entry.UserId,
            Username = entry.Username,
            TerritoryScore = entry.TerritoryScore,
            TotalDistance = (long)entry.TotalDistance,
            AveragePace = Math.Round(entry.AveragePace, 2),  // Round to 2 decimal places
            CompletionSpeed = entry.CompletionSpeed
        }).ToList();

        return ranked;
    }
}
