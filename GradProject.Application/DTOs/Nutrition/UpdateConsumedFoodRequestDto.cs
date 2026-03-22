using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Nutrition
{
    public class UpdateConsumedFoodRequestDto
    {
        public decimal PortionG { get; set; }
        public DateTime? ConsumedAt { get; set; }

        // Opsiyonel — gönderilirse öğün değiştirilir
        public MealType? MealType { get; set; }
    }
}