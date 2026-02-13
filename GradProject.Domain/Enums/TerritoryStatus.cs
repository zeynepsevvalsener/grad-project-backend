namespace GradProject.Domain.Enums
{
    /// <summary>
    /// Kullanıcının bir bölgedeki durumu.
    /// </summary>
    public enum TerritoryStatus
    {
        /// <summary>Bölge henüz açılmadı (unlock şartları sağlanmadı)</summary>
        Locked = 0,

        /// <summary>Bölge açıldı, sahiplenme ilerlemesi başlamadı</summary>
        Unlocked = 1,

        /// <summary>Sahiplenme ilerlemesi devam ediyor</summary>
        InProgress = 2,

        /// <summary>Bölge sahiplenildi (ownership tamamlandı)</summary>
        Owned = 3
    }
}
