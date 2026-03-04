namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// Computes repeat (diminishing returns) multiplier. Pluggable so the model can be changed without touching the engine.
/// </summary>
public interface IRepeatEffectCalculator
{
    /// <summary>
    /// Returns multiplier in (0, 1]. Example: 1 / (1 + previousContributionCount * repeatDecayFactor).
    /// </summary>
    double GetMultiplier(int previousContributionCount, double repeatDecayFactor);
}
