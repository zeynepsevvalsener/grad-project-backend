namespace GradProject.Domain.Enums;

/// <summary>
/// Type of ownership change recorded in territory_ownership_history.
/// </summary>
public enum OwnershipActionType
{
    /// <summary>User claimed an unowned territory.</summary>
    Claim = 0,

    /// <summary>Owner defended and updated score snapshot.</summary>
    Defend = 1,

    /// <summary>Previous owner lost territory (e.g. via invade).</summary>
    Lose = 2,

    /// <summary>Ownership transferred to new user (invade).</summary>
    Transfer = 3
}
