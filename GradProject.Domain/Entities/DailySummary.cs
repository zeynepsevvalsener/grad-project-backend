namespace GradProject.Domain.Entities
{
    public class DailySummary
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public DateOnly Date { get; set; }

        public int TotalIntakeCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalFat { get; set; }

        public User User { get; set; } = null!;
    }
}

