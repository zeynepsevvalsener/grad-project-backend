using GradProject.Application.DTOs.Nutrition.AI;

namespace GradProject.Application.Interfaces.Nutrition.AI
{
    public interface IMealParsingService
    {
        Task<MealParseResultDto> ParseAsync(int userId, MealParseRequestDto request, CancellationToken ct = default);

        Task SyncFoodsToAiAsync(CancellationToken ct = default);
        Task<string?> GetDailyFeedbackAsync(object payload, CancellationToken ct);
    }
}