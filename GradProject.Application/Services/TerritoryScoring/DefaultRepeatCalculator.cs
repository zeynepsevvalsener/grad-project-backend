namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// Default diminishing-returns: 1 / (1 + previousContributionCount * repeatDecayFactor).
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
