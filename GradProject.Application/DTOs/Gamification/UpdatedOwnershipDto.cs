namespace GradProject.Application.DTOs.Gamification;

public class UpdatedOwnershipDto
{
    public int TerritoryId { get; set; }
    public int NewOwnerUserId { get; set; }
    public int? PreviousOwnerUserId { get; set; }
    public string ActionType { get; set; } = null!; // CLAIM | TRANSFER
}
