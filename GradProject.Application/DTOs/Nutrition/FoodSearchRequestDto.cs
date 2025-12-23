namespace GradProject.Application.DTOs.Nutrition
{
    public class FoodSearchRequestDto
    {
        public string? Q { get; set; }          // user input: "apple", "ap", "fruit", etc.
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
