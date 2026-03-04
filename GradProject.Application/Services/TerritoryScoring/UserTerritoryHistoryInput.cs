namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// History of a user's previous contributions to a specific territory.
/// Used by the repeat-run decay calculation.
/// </summary>
public sealed class UserTerritoryHistoryInput
{
    public int UserId { get; init; }
    public int TerritoryId { get; init; }

    /// <summary>Number of previous contributions (runs) by this user to this territory.</summary>
    public int PreviousContributionCount { get; init; }
}
