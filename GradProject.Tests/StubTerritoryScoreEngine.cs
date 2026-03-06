using GradProject.Application.Interfaces.Gamification;
using GradProject.Application.Models;

namespace GradProject.Tests;

public sealed class StubTerritoryScoreEngine : ITerritoryScoreEngine
{
    private readonly IReadOnlyList<TerritoryContribution> _contributions;

    public StubTerritoryScoreEngine(IReadOnlyList<TerritoryContribution> contributions)
    {
        _contributions = contributions;
    }

    public Task<IReadOnlyList<TerritoryContribution>> ComputeRunContributionsAsync(
        int runId,
        IReadOnlyList<int> territoryIds,
        CancellationToken ct = default)
    {
        var byId = _contributions.Where(c => territoryIds.Contains(c.TerritoryId)).ToList();
        return Task.FromResult<IReadOnlyList<TerritoryContribution>>(byId);
    }
}
