namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// Configurable formula parameters (Task 3). Engine uses these weights and does not hardcode formula details.
/// </summary>
public sealed class ScoringFormulaConfig
{
    public double DistanceWeight { get; init; }
    public double CoverageWeight { get; init; }
    public double PaceWeight { get; init; }
    public double CompletionWeight { get; init; }
    public double RepeatDecayFactor { get; init; }
    public bool TerritoryWeightEnabled { get; init; }
}
