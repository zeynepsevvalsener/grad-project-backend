using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Nutrition
{
    public class UpdateMealRequestDto
    {
        public MealType MealType { get; set; }
        public DateTime LoggedAt { get; set; }
        public string? RawText { get; set; }
        public string? Notes { get; set; }
        public List<MealFoodDto>? Foods { get; set; }
    }
}

