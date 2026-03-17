namespace GradProject.Domain.Entities
{
    /// <summary>
    /// Represents a badge earned by a user. One row per user-badge pair; duplicate awards are prevented by a unique constraint.
    /// EarnedAtUtc is always stored in UTC. Future repeatable badges may require schema/constraint changes.
    /// </summary>
    public class UserBadge
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BadgeId { get; set; }

        /// <summary>When the badge was earned (UTC).</summary>
        public DateTime EarnedAtUtc { get; set; }

        public User User { get; set; } = null!;
        public Badge Badge { get; set; } = null!;
    }
}
