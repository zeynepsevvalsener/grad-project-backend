using GradProject.Domain.Enums;

namespace GradProject.Domain.Entities
{
    /// <summary>
    /// Bölgenin unlock edilmesi için gereken şart.
    /// Birden fazla şart olabilir; RequiresAll true ise hepsi sağlanmalı.
    /// </summary>
    public class TerritoryUnlockCondition
    {
        public int Id { get; set; }
        public int TerritoryId { get; set; }

        public TerritoryUnlockType UnlockType { get; set; }

        /// <summary>Hedef değer (örn. 10 koşu, 50 km)</summary>
        public double TargetValue { get; set; }

        /// <summary>BadgeEarned veya ChallengeCompleted için ilgili entity Id</summary>
        public int? RelatedEntityId { get; set; }

        /// <summary>Birden fazla şart varsa: true = hepsi, false = biri yeterli</summary>
        public bool RequiresAll { get; set; } = true;

        public int DisplayOrder { get; set; }

        // Navigation
        public Territory Territory { get; set; } = null!;
    }
}
