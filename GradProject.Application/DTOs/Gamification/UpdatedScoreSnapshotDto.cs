namespace GradProject.Application.DTOs.Gamification;

public class UpdatedScoreSnapshotDto
{
    public int TerritoryId { get; set; }
    public double OldScore { get; set; }
    public double NewScore { get; set; }
}
