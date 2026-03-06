namespace GradProject.Application.DTOs.Gamification;

public class ClaimTerritoryResponseDto
{
    public IReadOnlyList<int> ClaimedTerritories { get; set; } = new List<int>();
    public IReadOnlyList<RejectedTerritoryDto> RejectedTerritories { get; set; } = new List<RejectedTerritoryDto>();
    public IReadOnlyList<UpdatedOwnershipDto> UpdatedOwnership { get; set; } = new List<UpdatedOwnershipDto>();
    public IReadOnlyList<TerritoryEventDto> EventsToEmit { get; set; } = new List<TerritoryEventDto>();
}
