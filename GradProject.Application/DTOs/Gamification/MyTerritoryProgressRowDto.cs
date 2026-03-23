using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Gamification;

/// <summary>
/// Per-user territory row for GET /api/v1/territory/me.
/// </summary>
public sealed class MyTerritoryProgressRowDto
{
    public Guid TerritoryId { get; init; }
    public string Name { get; init; } = null!;

    /// <summary>Derived from progress, ownership target, and stored status (see territory rules).</summary>
    public TerritoryStatus Status { get; init; }

    public int ProgressPercent { get; init; }
    public DateTime? OwnedAt { get; init; }
    public bool IsCurrentOwner { get; init; }
}
