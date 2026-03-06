namespace GradProject.Application.DTOs.Gamification;

public class DefendTerritoryResponseDto
{
    public IReadOnlyList<int> DefendedTerritories { get; set; } = new List<int>();
    public IReadOnlyList<RejectedTerritoryDto> RejectedTerritories { get; set; } = new List<RejectedTerritoryDto>();
    public IReadOnlyList<UpdatedScoreSnapshotDto> UpdatedScoreSnapshots { get; set; } = new List<UpdatedScoreSnapshotDto>();
    public IReadOnlyList<TerritoryEventDto> EventsToEmit { get; set; } = new List<TerritoryEventDto>();
}
