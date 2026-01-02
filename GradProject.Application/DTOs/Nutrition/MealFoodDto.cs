namespace GradProject.Application.DTOs.Nutrition
{
    public class MealFoodDto
    {
        public int FoodId { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = null!;
    }
}

