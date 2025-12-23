namespace GradProject.Application.DTOs.Nutrition
{
    public class FoodSearchItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;

        public decimal Kcal { get; set; }
        public decimal ProteinG { get; set; }
        public decimal FatG { get; set; }
        public decimal CarbG { get; set; }

        public decimal? SugarG { get; set; }
        public decimal? FiberG { get; set; }
        public decimal? SodiumMg { get; set; }

        public decimal DefaultPortionG { get; set; }

        public string Source { get; set; } = null!;
        public string[] Aliases { get; set; } = Array.Empty<string>();
    }
}
