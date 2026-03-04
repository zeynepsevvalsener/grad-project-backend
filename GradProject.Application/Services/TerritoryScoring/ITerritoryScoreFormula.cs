namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// Strategy for computing base score from run metrics and coverage.
/// Pluggable so formula details can change without touching the engine.
/// </summary>
public interface ITerritoryScoreFormula
{
    /// <summary>
    /// Computes base score (expected range [0, 1]) from distance, coverage, pace, and completion
    /// components weighted by config.
    /// </summary>
    double ComputeBaseScore(
        RunInput run,
        double coverageRatio,
        TerritoryInput territory,
        ScoringFormulaConfig config);
}
