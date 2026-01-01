using GradProject.Application.DTOs.Nutrition.AI;

namespace GradProject.Application.Interfaces.Nutrition.AI
{
    public interface IMealParsingService
    {
        Task<MealParseResultDto> ParseAsync(int userId, MealParseRequestDto request, CancellationToken ct = default);
    }
}
