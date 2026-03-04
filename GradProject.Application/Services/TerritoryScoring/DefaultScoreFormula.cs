namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// Default base score formula: weighted sum of four normalized [0,1] components.
/// BaseScore = w_d*D_norm + w_c*C_effect + w_p*P_norm + w_t*T_norm
///
/// See docs/HLN-8-Territory-Scoring-Model.md sections 3-6 for rationale.
/// </summary>
public sealed class DefaultScoreFormula : ITerritoryScoreFormula
{
    private const double DistanceCapMeters = 20_000;
    private const double PaceMinSecPerKm = 180;   // 3 min/km  -> P_norm = 1
    private const double PaceMaxSecPerKm = 600;   // 10 min/km -> P_norm = 0
    private const double CompletionCapSeconds = 7200; // 2 hours

    public double ComputeBaseScore(
        RunInput run,
        double coverageRatio,
        TerritoryInput territory,
        ScoringFormulaConfig config)
    {
        double dComp = NormalizeDistance(run.DistanceMeters);
        double cComp = ComputeCoverageEffect(coverageRatio, config.CoverageExponent);
        double pComp = NormalizePace(run.AveragePace);
        double tComp = NormalizeCompletion(run.CompletionDurationSeconds);

        return dComp * config.DistanceWeight
             + cComp * config.CoverageWeight
             + pComp * config.PaceWeight
             + tComp * config.CompletionWeight;
    }

    /// <summary>D_norm = min(1, distanceMeters / DistanceCapMeters). Cap-based linear.</summary>
    private static double NormalizeDistance(double distanceMeters)
    {
        if (distanceMeters <= 0) return 0;
        return Math.Min(1.0, distanceMeters / DistanceCapMeters);
    }

    /// <summary>C_effect = coverageRatio^alpha, clamped to [0,1].</summary>
    private static double ComputeCoverageEffect(double coverageRatio, double alpha)
    {
        double clamped = Math.Clamp(coverageRatio, 0, 1);
        return Math.Pow(clamped, alpha);
    }

    /// <summary>Faster pace (lower s/km) = higher value. Absolute band normalization.</summary>
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
