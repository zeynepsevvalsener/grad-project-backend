namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// Engine input for a single run. Maps from HLN-7 Map Running Region output (covered cells) and run metrics.
/// </summary>
public sealed class RunInput
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public double DistanceMeters { get; init; }
    /// <summary>Average pace in seconds per kilometer (s/km).</summary>
    public double AveragePace { get; init; }
    /// <summary>Completion duration in seconds.</summary>
    public int CompletionDurationSeconds { get; init; }
    /// <summary>Cell IDs covered by this run (HLN-7 region extraction output). Empty = no coverage. Used when territory.CellIds is provided.</summary>
    public IReadOnlyCollection<string> CoveredCellIds { get; init; } = Array.Empty<string>();

    /// <summary>When HLN-7 provides per-territory unique cell count, use this instead of intersecting CoveredCellIds with territory.CellIds. Key = territory Id, Value = unique covered cell count in that territory.</summary>
    public IReadOnlyDictionary<int, int>? CoveredCellCountByTerritoryId { get; init; }
}
