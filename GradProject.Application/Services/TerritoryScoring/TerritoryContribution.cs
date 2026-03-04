namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// Per-territory contribution from a single run.
/// Consumed by claim/defend logic, challenge progress update, and achievement triggers.
/// </summary>
public sealed class TerritoryContribution
{
    public int TerritoryId { get; init; }
    public double BaseScore { get; init; }
    public double RepeatMultiplier { get; init; }
    public double FinalScore { get; init; }
    public double CoverageRatio { get; init; }
    public double DistanceInTerritory { get; init; }

    /// <summary>Average pace (s/km) for this run; used when aggregating user territory score.</summary>
    public double AveragePace { get; init; }
}
