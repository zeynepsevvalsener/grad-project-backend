namespace GradProject.Application.DTOs.Nutrition.AI
{
    public class MealParseResultDto
    {
        public string OriginalText { get; set; } = null!;
        public DateTime? ConsumedAt { get; set; }

        public List<MealParseItemDto> Items { get; set; } = new();

        public decimal TotalKcal { get; set; }
        public decimal TotalProteinG { get; set; }
        public decimal TotalFatG { get; set; }
        public decimal TotalCarbG { get; set; }

        public string? Feedback { get; set; }
    }
}
