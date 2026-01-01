namespace GradProject.Application.DTOs.Nutrition.AI
{
    public class MealParseRequestDto
    {
        public string Text { get; set; } = null!;

        // Optional: if user types a date/time, frontend can send it
        public DateTime? ConsumedAt { get; set; }
    }
}
