namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// Default base score: weighted sum of distance, coverage, pace, completion components (each normalized 0..1).
/// Formula details are encapsulated here; engine only calls ComputeBaseScore.
/// </summary>
public sealed class DefaultScoreFormula : ITerritoryScoreFormula
{
    // Normalization bounds for 0..1 components (config could override these in a future formula variant)
    private const double DistanceCapMeters = 20_000;
    private const double PaceMinSecPerKm = 180;  // 3 min/km
    private const double PaceMaxSecPerKm = 600;  // 10 min/km
    private const double CompletionCapSeconds = 7200; // 2 hours

    public double ComputeBaseScore(
        RunInput run,
        double coverageRatio,
        TerritoryInput territory,
        ScoringFormulaConfig config)
    {
        double dComp = NormalizeDistance(run.DistanceMeters);
        double cComp = Math.Clamp(coverageRatio, 0, 1);
        double pComp = NormalizePace(run.AveragePace);
        double tComp = NormalizeCompletion(run.CompletionDurationSeconds);

        return dComp * config.DistanceWeight
             + cComp * config.CoverageWeight
             + pComp * config.PaceWeight
             + tComp * config.CompletionWeight;
    }

    private static double NormalizeDistance(double distanceMeters)
    {
        if (distanceMeters <= 0) return 0;
        return Math.Min(1.0, distanceMeters / DistanceCapMeters);
    }

    /// <summary>Faster pace (lower s/km) = higher value.</summary>
    private static double NormalizePace(double paceSecPerKm)
    {
        if (paceSecPerKm <= PaceMinSecPerKm) return 1.0;
        if (paceSecPerKm >= PaceMaxSecPerKm) return 0.0;
        return (PaceMaxSecPerKm - paceSecPerKm) / (PaceMaxSecPerKm - PaceMinSecPerKm);
    }

    /// <summary>Faster completion (lower seconds) = higher value.</summary>
    private static double NormalizeCompletion(double completionSeconds)
    {
        if (completionSeconds <= 0) return 1.0;
        if (completionSeconds >= CompletionCapSeconds) return 0.0;
        return 1.0 - (completionSeconds / CompletionCapSeconds);
    }
}
