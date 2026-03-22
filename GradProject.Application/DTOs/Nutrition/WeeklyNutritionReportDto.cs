namespace GradProject.Application.DTOs.Nutrition
{
    public class WeeklyNutritionReportDto
    {
        public DateOnly WeekStart { get; set; }
        public DateOnly WeekEnd { get; set; }

        // Kaç gün veri var (giriş yapılmayan günler dahil değil)
        public int TrackedDays { get; set; }

        // Haftalık ortalama skor (0-10)
        public decimal AverageScore { get; set; }

        // Günlük detaylar
        public List<DailyNutritionScoreDto> Days { get; set; } = new();
    }

    public class DailyNutritionScoreDto
    {
        public DateOnly Date { get; set; }

        // Tüketim
        public int TotalIntakeCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalFat { get; set; }

        // Hedefler (snapshot)
        public int? CalorieTarget { get; set; }
        public decimal? ProteinTargetG { get; set; }
        public decimal? CarbTargetG { get; set; }
        public decimal? FatTargetG { get; set; }

        // Günlük skor (0-10)
        public decimal DailyScore { get; set; }

        // Metrik bazlı puanlar (frontend breakdown için)
        public decimal CalorieScore { get; set; }
        public decimal ProteinScore { get; set; }
        public decimal CarbScore { get; set; }
        public decimal FatScore { get; set; }
    }
}