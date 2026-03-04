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
        // Always prioritize TerritoryScore as the primary sorting rule
        var ordered = aggregatedData
            .OrderByDescending(x => x.TerritoryScore ?? 0)
            .ThenByDescending(x => metric == ChallengeMetric.Distance ? x.TotalDistance : 0)
            .ThenBy(x => metric == ChallengeMetric.Pace ? x.AveragePace : double.MaxValue)
            .ThenBy(x => metric == ChallengeMetric.Duration ? (x.CompletionSpeed ?? long.MaxValue) : long.MaxValue)
            .ThenByDescending(x => x.TotalDistance)
            .ThenBy(x => x.AveragePace)
            .ThenBy(x => x.CompletionSpeed ?? long.MaxValue)
            .ThenBy(x => x.UserId);

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
