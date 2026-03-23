using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Nutrition.AI
{
    public class MealParseRequestDto
    {
        public string Text { get; set; } = null!;
        public DateTime? ConsumedAt { get; set; }
        public string? Language { get; set; }

        // AI'dan gelirse kullan, gelmezse saate göre belirle
        public MealType? MealType { get; set; }
    }
}