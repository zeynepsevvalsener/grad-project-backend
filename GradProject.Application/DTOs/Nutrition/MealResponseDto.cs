using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Nutrition
{
    public class MealResponseDto
    {
        public int Id { get; set; }
        public MealType MealType { get; set; }
        public string MealTypeName { get; set; } = null!;
        public DateTime LoggedAt { get; set; }
        public string? RawText { get; set; }
        public string? Notes { get; set; }

        public List<MealFoodResponseDto> Foods { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

