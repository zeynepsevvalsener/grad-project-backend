// Application/DTOs/Nutrition/Admin/AdminUpdateFoodRequestDto.cs

namespace GradProject.Application.DTOs.Nutrition.Admin
{
    public class AdminUpdateFoodRequestDto
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public decimal? Kcal { get; set; }
        public decimal? ProteinG { get; set; }
        public decimal? FatG { get; set; }
        public decimal? CarbG { get; set; }
        public decimal? SugarG { get; set; }
        public decimal? FiberG { get; set; }
        public decimal? SodiumMg { get; set; }
        public decimal? DefaultPortionG { get; set; }
        public string? Source { get; set; }

        // null gelirse alias'lara dokunma, liste gelirse replace et
        public List<FoodAliasDto>? Aliases { get; set; }
    }
}