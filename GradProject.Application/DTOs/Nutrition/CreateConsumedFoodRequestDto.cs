namespace GradProject.Application.DTOs.Nutrition
{
    public class CreateConsumedFoodRequestDto
    {
        public int FoodId { get; set; }
        public decimal PortionG { get; set; }

        // Client can send; if null, we’ll set to UtcNow in service/controller
        public DateTime? ConsumedAt { get; set; }
    }
}
