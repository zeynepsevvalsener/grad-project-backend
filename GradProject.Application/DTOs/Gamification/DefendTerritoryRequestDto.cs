namespace GradProject.Application.DTOs.Gamification;

public class DefendTerritoryRequestDto
{
    public int RunId { get; set; }
    public IReadOnlyList<Guid> TerritoryIds { get; set; } = new List<Guid>();
}
