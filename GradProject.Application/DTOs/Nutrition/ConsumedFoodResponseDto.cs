namespace GradProject.Application.DTOs.Nutrition
{
    public class ConsumedFoodResponseDto
    {
        public int Id { get; set; }

        public int FoodId { get; set; }
        public string FoodName { get; set; } = null!;

        public DateTime ConsumedAt { get; set; }
        public decimal PortionG { get; set; }
    }
}
