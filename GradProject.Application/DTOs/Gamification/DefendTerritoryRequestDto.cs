namespace GradProject.Application.DTOs.Gamification;

public class DefendTerritoryRequestDto
{
    public int RunId { get; set; }
    public IReadOnlyList<int> TerritoryIds { get; set; } = new List<int>();
}
