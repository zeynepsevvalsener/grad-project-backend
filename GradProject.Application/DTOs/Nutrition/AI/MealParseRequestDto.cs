namespace GradProject.Application.DTOs.Nutrition.AI
{
    public class MealParseRequestDto
    {
        public string Text { get; set; } = null!;

        public DateTime? ConsumedAt { get; set; }

        // "tr" / "en" / "tr-TR" vs. (resolver zaten normalize ediyor)
        public string? Language { get; set; }
    }
}
