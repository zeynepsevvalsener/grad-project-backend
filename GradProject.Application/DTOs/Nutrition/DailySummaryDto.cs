namespace GradProject.Application.DTOs.Nutrition
{
    public class DailySummaryDto
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }

        // Tüketim
        public int TotalIntakeCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalFat { get; set; }

        // O güne ait snapshot hedefler
        public int? CalorieTarget { get; set; }
        public decimal? ProteinTargetG { get; set; }
        public decimal? CarbTargetG { get; set; }
        public decimal? FatTargetG { get; set; }

        public int? BurnedCalories { get; set; }
        public string? AiFeedback { get; set; }
    }
}