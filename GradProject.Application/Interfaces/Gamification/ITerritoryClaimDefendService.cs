using GradProject.Application.DTOs.Gamification;

namespace GradProject.Application.Interfaces.Gamification;

/// <summary>
/// HLN-8: Claim and defend territory ownership by run.
/// </summary>
public interface ITerritoryClaimDefendService
{
    Task<ClaimTerritoryResponseDto> ClaimAsync(
        int userId,
        int runId,
        IReadOnlyList<int> territoryIds,
        CancellationToken ct = default);

    Task<DefendTerritoryResponseDto> DefendAsync(
        int userId,
        int runId,
        IReadOnlyList<int> territoryIds,
        CancellationToken ct = default);
}
