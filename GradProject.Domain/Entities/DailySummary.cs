namespace GradProject.Domain.Entities
{
    public class DailySummary
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateOnly Date { get; set; }

        // Tüketim
        public int TotalIntakeCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalFat { get; set; }

        // Hedefler — o gün snapshot'ý, profil deðiþse bile korunur
        public int? CalorieTarget { get; set; }
        public decimal? ProteinTargetG { get; set; }
        public decimal? CarbTargetG { get; set; }
        public decimal? FatTargetG { get; set; }

        public User User { get; set; } = null!;
    }
}