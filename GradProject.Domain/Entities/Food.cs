namespace GradProject.Domain.Entities
{
    public class Food
    {
        public int Id { get; set; }

        // Display name (e.g., "Apple")
        public string Name { get; set; } = null!;

        // e.g., "fruit", "meat", "beverage" (MVP: string)
        public string Category { get; set; } = null!;

        // Nutrition values per 100 grams
        public decimal Kcal { get; set; }

        public decimal ProteinG { get; set; }
        public decimal FatG { get; set; }
        public decimal CarbG { get; set; }

        public decimal SugarG { get; set; }
        public decimal FiberG { get; set; }

        public decimal SodiumMg { get; set; }

        // Default portion size in grams (e.g., 182g for one medium apple)
        public decimal DefaultPortionG { get; set; }

        // e.g., "USDA"
        public string Source { get; set; } = null!;

        // Turkish aliases etc. (PostgreSQL text[])
        public string[] Aliases { get; set; } = [];
    }
}
