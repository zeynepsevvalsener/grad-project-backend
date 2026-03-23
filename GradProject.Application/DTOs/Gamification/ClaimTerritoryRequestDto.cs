namespace GradProject.Application.DTOs.Gamification;

public class ClaimTerritoryRequestDto
{
    public int RunId { get; set; }
    public IReadOnlyList<Guid> TerritoryIds { get; set; } = new List<Guid>();
}
