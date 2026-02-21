namespace GradProject.Application.DTOs.Nutrition
{
    public class FoodResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Source { get; set; } = null!;

        // Per 100 grams
        public decimal Kcal { get; set; }
        public decimal ProteinG { get; set; }
        public decimal FatG { get; set; }
        public decimal CarbG { get; set; }
        public decimal SugarG { get; set; }
        public decimal FiberG { get; set; }
        public decimal SodiumMg { get; set; }

        public decimal DefaultPortionG { get; set; }

    }
}
