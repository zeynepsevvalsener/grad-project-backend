using GradProject.Domain.Enums;

namespace GradProject.Domain.Entities
{
    public class Meal
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public MealType MealType { get; set; }
        public string? RawText { get; set; }
        public string? Notes { get; set; }
        public DateTime LoggedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<MealFood> MealFoods { get; set; } = new List<MealFood>();
    }
}

