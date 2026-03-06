namespace GradProject.Application.Models;

/// <summary>
/// Per-run, per-territory score output from the Territory Score Engine (HLN-8).
/// </summary>
public class TerritoryContribution
{
    public int TerritoryId { get; set; }
    public double FinalScore { get; set; }
    public double CoverageRatio { get; set; }
    public double DistanceInTerritory { get; set; }
}
