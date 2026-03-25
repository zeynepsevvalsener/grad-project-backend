using GradProject.Domain.Entities;
using GradProject.Domain.Enums;

namespace GradProject.Application.Services.Gamification;

/// <summary>
/// Derives API-facing territory status from persisted progress and ownership target.
/// </summary>
public static class TerritoryProgressRules
{
    /// <summary>
    /// Locked: no row or explicit locked with no progress.
    /// Owned: progress &gt;= ownership target AND user is the current territory owner.
    /// InProgress: progress &gt; 0 but user is not (or no longer) the current owner.
    /// Unlocked: row exists, progress 0, not locked.
    /// </summary>
    public static TerritoryStatus ResolveDisplayStatus(UserTerritory? row, int ownershipTargetPercent, bool isCurrentOwner = false)
    {
        if (row == null)
            return TerritoryStatus.Locked;

        if (row.ProgressPercent >= ownershipTargetPercent && isCurrentOwner)
            return TerritoryStatus.Owned;

        if (row.ProgressPercent > 0)
            return TerritoryStatus.InProgress;

        if (row.ProgressPercent == 0)
        {
            if (row.Status == TerritoryStatus.Locked)
                return TerritoryStatus.Locked;
            return TerritoryStatus.Unlocked;
        }

        return TerritoryStatus.InProgress;
    }
}
