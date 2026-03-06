namespace GradProject.Application.DTOs.Gamification;

public class ClaimTerritoryRequestDto
{
    public int RunId { get; set; }
    public IReadOnlyList<int> TerritoryIds { get; set; } = new List<int>();
}
