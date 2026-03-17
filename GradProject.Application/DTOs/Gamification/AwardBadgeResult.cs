namespace GradProject.Application.DTOs.Gamification
{
    /// <summary>
    /// Result of attempting to award a badge to a user (idempotent: AlreadyExists when already awarded).
    /// </summary>
    public enum AwardBadgeResult
    {
        Created,
        AlreadyExists,
        UserNotFound,
        BadgeNotFound,
        Failed
    }
}
