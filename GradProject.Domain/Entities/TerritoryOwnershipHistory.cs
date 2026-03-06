using GradProject.Domain.Enums;

namespace GradProject.Domain.Entities;

/// <summary>
/// Audit log for territory ownership changes (claim, defend, lose, transfer).
/// Used for leaderboard, achievements, and notifications.
/// </summary>
public class TerritoryOwnershipHistory
{
    public long Id { get; set; }
    public int TerritoryId { get; set; }
    public int? PreviousOwnerUserId { get; set; }
    public int NewOwnerUserId { get; set; }
    public OwnershipActionType ActionType { get; set; }
    public int ActionRunId { get; set; }
    public decimal ActionScore { get; set; }
    public DateTime ActionAt { get; set; }
    public string? Metadata { get; set; }

    public Territory Territory { get; set; } = null!;
    public User? PreviousOwnerUser { get; set; }
    public User NewOwnerUser { get; set; } = null!;
    public RunningActivity ActionRun { get; set; } = null!;
}
