namespace GradProject.Application.DTOs.Gamification;

/// <summary>
/// Event contract for achievement/notification (HLN-8). Event bus not implemented; payload only.
/// </summary>
public class TerritoryEventDto
{
    public string EventType { get; set; } = null!; // TERRITORY_CLAIMED | TERRITORY_DEFENDED | TERRITORY_LOST | TERRITORY_TRANSFERRED
    public int UserId { get; set; }
    public int TerritoryId { get; set; }
    public int RunId { get; set; }
    public DateTime ActionAt { get; set; }
    public double Score { get; set; }
    public int? PreviousOwnerUserId { get; set; }
    public int? NewOwnerUserId { get; set; }
}
