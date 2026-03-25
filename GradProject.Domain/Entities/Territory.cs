namespace GradProject.Domain.Entities
{
    /// <summary>
    /// Gamification bölge (territory) tanımı.
    /// Kullanıcılar unlock şartlarını sağlayarak bölgeyi açar ve progress ile sahiplenir.
    /// İleride coğrafi alan/koordinat genişletmesi yapılabilir.
    /// </summary>
    public class Territory
    {
        /// <summary>Surrogate key (internal FKs, claim/defend APIs).</summary>
        public int Id { get; set; }

        /// <summary>Stable public identifier for clients (API <c>id</c>).</summary>
        public Guid PublicId { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        /// <summary>Bölge kodu/identifikatör (ileride harita entegrasyonu için)</summary>
        public string? RegionCode { get; set; }

        /// <summary>Listeleme sırası</summary>
        public int DisplayOrder { get; set; }

        public string? IconUrl { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Sahiplenme için gerekli progress hedefi (0-100). 100 = tam sahiplenme.</summary>
        public int OwnershipTargetPercent { get; set; } = 100;

        /// <summary>Region extraction'dan gelen cell set (JSONB).</summary>
        public string? GeometryCells { get; set; }

        /// <summary>HLN-8: Current owner user Id; null if unclaimed.</summary>
        public int? CurrentOwnerUserId { get; set; }

        /// <summary>HLN-8: When current owner claimed.</summary>
        public DateTime? CurrentOwnerSince { get; set; }

        /// <summary>HLN-8: Score snapshot at claim/defend.</summary>
        public decimal? CurrentOwnerScoreSnapshot { get; set; }

        /// <summary>HLN-8: Optimistic locking version.</summary>
        public int Version { get; set; }

        // Navigation
        public User? CurrentOwnerUser { get; set; }
        public ICollection<TerritoryUnlockCondition> UnlockConditions { get; set; } = new List<TerritoryUnlockCondition>();
        /// <summary>Per-user progress (UserTerritoryProgress aggregate; table <c>UserTerritories</c>).</summary>
        public ICollection<UserTerritory> UserTerritories { get; set; } = new List<UserTerritory>();
        public ICollection<TerritoryCell> TerritoryCells { get; set; } = new List<TerritoryCell>();
        public ICollection<TerritoryOwnershipHistory> OwnershipHistory { get; set; } = new List<TerritoryOwnershipHistory>();
    }
}
