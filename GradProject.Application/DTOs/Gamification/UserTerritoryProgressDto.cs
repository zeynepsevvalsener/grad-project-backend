using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Gamification
{
    /// <summary>
    /// Kullanıcının bir bölgedeki ilerlemesi ve ownership durumu.
    /// </summary>
    public class UserTerritoryProgressDto
    {
        public int TerritoryId { get; set; }
        public string TerritoryName { get; set; } = null!;
        public string? TerritoryIconUrl { get; set; }

        public TerritoryStatus Status { get; set; }
        public string StatusName { get; set; } = null!;

        /// <summary>Sahiplenme ilerlemesi (0-100)</summary>
        public int ProgressPercent { get; set; }

        public int OwnershipTargetPercent { get; set; }

        public DateTime? UnlockedAt { get; set; }
        public DateTime? OwnedAt { get; set; }
        public DateTime? LastActivityAt { get; set; }
    }
}
