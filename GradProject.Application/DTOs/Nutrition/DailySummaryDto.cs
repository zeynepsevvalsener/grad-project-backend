namespace GradProject.Application.DTOs.Nutrition
{
    public class DailySummaryDto
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public int TotalIntakeCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalFat { get; set; }
        public int? BurnedCalories { get; set; }
        public string? AiFeedback { get; set; }
    }
}

