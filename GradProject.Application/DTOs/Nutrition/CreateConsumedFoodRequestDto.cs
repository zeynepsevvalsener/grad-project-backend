using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Nutrition
{
    public class CreateConsumedFoodRequestDto
    {
        public int FoodId { get; set; }
        public decimal PortionG { get; set; }
        public DateTime? ConsumedAt { get; set; }

        // Hangi öğüne ait olduğu — zorunlu
        public MealType MealType { get; set; }
    }
}