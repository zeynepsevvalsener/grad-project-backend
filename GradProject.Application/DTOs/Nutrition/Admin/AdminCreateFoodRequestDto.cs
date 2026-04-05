// Application/DTOs/Nutrition/Admin/AdminCreateFoodRequestDto.cs

namespace GradProject.Application.DTOs.Nutrition.Admin
{
    public class AdminCreateFoodRequestDto
    {
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal Kcal { get; set; }
        public decimal ProteinG { get; set; }
        public decimal FatG { get; set; }
        public decimal CarbG { get; set; }
        public decimal SugarG { get; set; }
        public decimal FiberG { get; set; }
        public decimal SodiumMg { get; set; }
        public decimal DefaultPortionG { get; set; }
        public string Source { get; set; } = null!;

        // Opsiyonel, boş liste gönderilebilir
        public List<FoodAliasDto> Aliases { get; set; } = new();
    }

    public class FoodAliasDto
    {
        public string Language { get; set; } = "en"; // "tr" | "en"
        public string Alias { get; set; } = null!;
    }
}