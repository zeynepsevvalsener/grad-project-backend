namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// Per-user, per-territory history for repeat (diminishing returns) calculation.
/// </summary>
public sealed class UserTerritoryHistoryInput
{
    public int UserId { get; init; }
    public int TerritoryId { get; init; }
    public double PreviousCoverageRatio { get; init; }
    public double PreviousTotalDistanceInTerritory { get; init; }
    public int PreviousContributionCount { get; init; }
}
