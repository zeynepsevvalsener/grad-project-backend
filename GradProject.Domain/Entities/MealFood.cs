namespace GradProject.Domain.Entities
{
    public class MealFood
    {
        public int Id { get; set; }
        public int MealId { get; set; }
        public int FoodId { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = null!;

        // Navigation properties
        public Meal Meal { get; set; } = null!;
        public Food Food { get; set; } = null!;
    }
}

