namespace GradProject.Application.DTOs.Gamification;

/// <summary>
/// Event contract for achievement/notification (HLN-8). Event bus not implemented; payload only.
/// </summary>
public class TerritoryEventDto
{
    /// <summary>Known values defined in <see cref="EventTypes"/>.</summary>
    public string EventType { get; set; } = null!;

    public static class EventTypes
    {
        public const string Claimed     = "TERRITORY_CLAIMED";
        public const string Transferred = "TERRITORY_TRANSFERRED";
        public const string Defended    = "TERRITORY_DEFENDED";
        public const string Lost        = "TERRITORY_LOST";
    }
    public int UserId { get; set; }
    public int TerritoryId { get; set; }
    public int RunId { get; set; }
    public DateTime ActionAt { get; set; }
    public double Score { get; set; }
    public int? PreviousOwnerUserId { get; set; }
    public int? NewOwnerUserId { get; set; }
}
