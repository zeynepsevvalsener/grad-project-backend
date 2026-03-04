namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// Default diminishing-returns repeat multiplier: 1 / (1 + previousContributionCount * repeatDecayFactor).
/// Result is always in (0, 1]. See docs/HLN-8-Territory-Scoring-Model.md section 7.
/// </summary>
public sealed class DefaultRepeatCalculator : IRepeatEffectCalculator
{
    public double GetMultiplier(int previousContributionCount, double repeatDecayFactor)
    {
        if (previousContributionCount <= 0) return 1.0;
        if (repeatDecayFactor <= 0) return 1.0;
        return 1.0 / (1.0 + previousContributionCount * repeatDecayFactor);
    }
}
