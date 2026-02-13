namespace GradProject.Domain.Enums
{
    /// <summary>
    /// Bölgenin açılması için gerekli şart tipi.
    /// İleride yeni şart tipleri eklenebilir (örn. SocialUnlock, TimeBasedUnlock).
    /// </summary>
    public enum TerritoryUnlockType
    {
        /// <summary>Belirli sayıda koşu tamamlandığında</summary>
        RunCount = 1,

        /// <summary>Toplam mesafe (km) koşulduğunda</summary>
        TotalDistanceKm = 2,

        /// <summary>Belirli rozet kazanıldığında</summary>
        BadgeEarned = 3,

        /// <summary>Belirli challenge tamamlandığında</summary>
        ChallengeCompleted = 4,

        /// <summary>Belirli gün streak yapıldığında</summary>
        StreakDays = 5,

        /// <summary>Toplam puan kazanıldığında</summary>
        PointsEarned = 6
    }
}
