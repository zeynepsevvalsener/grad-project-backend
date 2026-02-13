namespace GradProject.Domain.Entities
{
    /// <summary>
    /// Gamification bölge (territory) tanımı.
    /// Kullanıcılar unlock şartlarını sağlayarak bölgeyi açar ve progress ile sahiplenir.
    /// İleride coğrafi alan/koordinat genişletmesi yapılabilir.
    /// </summary>
    public class Territory
    {
        public int Id { get; set; }
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

        // Navigation
        public ICollection<TerritoryUnlockCondition> UnlockConditions { get; set; } = new List<TerritoryUnlockCondition>();
        public ICollection<UserTerritory> UserTerritories { get; set; } = new List<UserTerritory>();
    }
}
