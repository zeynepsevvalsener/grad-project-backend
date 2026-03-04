namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// Engine input for a territory. Used for coverage ratio and optional weight.
/// </summary>
public sealed class TerritoryInput
{
    public int Id { get; init; }
    public int TotalCellCount { get; init; }

    /// <summary>
    /// Cell IDs belonging to this territory. Used to intersect with run.CoveredCellIds.
    /// Null when HLN-7 provides per-territory counts instead.
    /// </summary>
    public IReadOnlySet<string>? CellIds { get; init; }

    /// <summary>Optional weight applied when TerritoryWeightEnabled in config.</summary>
    public double? Weight { get; init; }
}
