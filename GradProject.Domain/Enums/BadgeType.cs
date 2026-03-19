namespace GradProject.Domain.Enums
{
public enum BadgeType
{
    FirstRun = 1,
    Streak = 2,
    PersonalBest = 3,
    Territory = 4,
    ChallengeCompletion = 5,

    /// <summary>Awarded when the user claims an unclaimed territory for the first time.</summary>
    TerritoryFirstClaim = 6,

    /// <summary>Awarded when the user successfully defends a territory they own.</summary>
    TerritoryDefender = 7,

    /// <summary>Awarded when the user takes over a territory previously owned by another user.</summary>
    TerritoryConqueror = 8,
}
}

