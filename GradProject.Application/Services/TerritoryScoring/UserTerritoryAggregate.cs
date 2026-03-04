namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// User-level aggregate from territory contributions.
/// Used for leaderboard (e.g. UserChallenge.TerritoryScore) and achievement triggers.
/// </summary>
public sealed class UserTerritoryAggregate
{
    public double TotalTerritoryScore { get; init; }
    public double TotalDistance { get; init; }

    /// <summary>Distance-weighted average pace in seconds per kilometer (s/km).</summary>
    public double AveragePace { get; init; }

    public int TerritoryCount { get; init; }
    public double StrongestTerritoryScore { get; init; }
}
