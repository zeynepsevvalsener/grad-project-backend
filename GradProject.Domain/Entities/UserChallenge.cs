namespace GradProject.Domain.Entities
{
    /// <summary>
    /// Kullanıcının challenge ile ilişkisi: join durumu, progress ve completion.
    /// </summary>
    public class UserChallenge
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ChallengeId { get; set; }

        /// <summary>Challenge'a join olunan tarih</summary>
        public DateTime JoinedAt { get; set; }

        /// <summary>İlerleme mesafesi (metre cinsinden) - Running challenge'lar için</summary>
        public long ProgressDistanceMeters { get; set; } = 0;

        /// <summary>İlerleme kalorisi - Nutrition challenge'lar için</summary>
        public int ProgressCalories { get; set; } = 0;

        /// <summary>Challenge tamamlandı mı?</summary>
        public bool Completed { get; set; } = false;

        /// <summary>Challenge tamamlandığında</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>Son güncelleme tarihi</summary>
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>Toplam süre (saniye) - Sprint 8/9 leaderboard için</summary>
        public long? TotalDurationSeconds { get; set; }

        /// <summary>Territory score - Sprint 8/9 leaderboard için</summary>
        public double? TerritoryScore { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public Challenge Challenge { get; set; } = null!;
    }
}

