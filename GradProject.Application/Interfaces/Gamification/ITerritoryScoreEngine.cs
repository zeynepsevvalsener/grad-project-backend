using GradProject.Application.Models;

namespace GradProject.Application.Interfaces.Gamification;

/// <summary>
/// Territory score computation engine (HLN-8). Computes per-run, per-territory contributions.
/// </summary>
public interface ITerritoryScoreEngine
{
    /// <summary>
    /// Computes territory contributions for a run over the given territories.
    /// Returns only territories that have positive coverage/contribution.
    /// </summary>
    Task<IReadOnlyList<TerritoryContribution>> ComputeRunContributionsAsync(
        int runId,
        IReadOnlyList<int> territoryIds,
        CancellationToken ct = default);
}
