namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// Configurable formula parameters for territory scoring.
/// Engine uses these weights; formula details are encapsulated in ITerritoryScoreFormula implementations.
/// </summary>
public sealed record ScoringFormulaConfig
{
    public double DistanceWeight { get; init; }
    public double CoverageWeight { get; init; }
    public double PaceWeight { get; init; }
    public double CompletionWeight { get; init; }

    /// <summary>Coverage exponent (alpha): C_effect = coverageRatio^CoverageExponent. 1.0 = linear.</summary>
    public double CoverageExponent { get; init; } = 1.0;

    public double RepeatDecayFactor { get; init; }
    public bool TerritoryWeightEnabled { get; init; }
}
