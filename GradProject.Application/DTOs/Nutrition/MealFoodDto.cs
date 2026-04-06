namespace GradProject.Application.DTOs.Nutrition
{
    public class MealFoodDto
    {
        public int Id { get; set; }
        public int FoodId { get; set; }
        public string FoodName { get; set; } = null!;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = null!;
        public decimal PortionG { get; set; }
        public string? DisplayName { get; set; }

    }
}
