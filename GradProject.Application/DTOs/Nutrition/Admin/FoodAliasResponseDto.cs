// Application/DTOs/Nutrition/Admin/FoodAliasResponseDto.cs

namespace GradProject.Application.DTOs.Nutrition.Admin
{
    public class FoodAliasResponseDto
    {
        public int Id { get; set; }
        public string Language { get; set; } = null!;
        public string Alias { get; set; } = null!;
        public string NormalizedAlias { get; set; } = null!;
    }
}