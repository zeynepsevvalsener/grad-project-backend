using GradProject.Domain.Enums;

namespace GradProject.Domain.Entities
{
    /// <summary>
    /// UserTerritoryProgress: kullanıcının bölge ile ilişkisi (unlock, progress, ownership).
    /// Kayıt yoksa bölge Locked kabul edilir. Table: UserTerritories.
    /// </summary>
    public class UserTerritory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TerritoryId { get; set; }

        public TerritoryStatus Status { get; set; } = TerritoryStatus.Locked;

        /// <summary>Bölge unlock edildiğinde</summary>
        public DateTime? UnlockedAt { get; set; }

        /// <summary>Sahiplenme ilerlemesi (0-100)</summary>
        public int ProgressPercent { get; set; }

        /// <summary>Bölge sahiplenildiğinde</summary>
        public DateTime? OwnedAt { get; set; }

        /// <summary>Son ilerleme aktivitesi (koşu, challenge vb.)</summary>
        public DateTime? LastActivityAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public Territory Territory { get; set; } = null!;
    }
}
