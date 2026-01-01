namespace GradProject.Application.DTOs.Nutrition.AI
{
    public class MealParseItemDto
    {
        public string Raw { get; set; } = null!;
        public string NormalizedName { get; set; } = null!;

        public decimal PortionG { get; set; }

        public int? MatchedFoodId { get; set; }
        public string? MatchedFoodName { get; set; }

        public decimal Confidence { get; set; }

        public decimal? Kcal { get; set; }
        public decimal? ProteinG { get; set; }
        public decimal? FatG { get; set; }
        public decimal? CarbG { get; set; }
    }
}
