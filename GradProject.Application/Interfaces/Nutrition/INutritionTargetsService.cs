using GradProject.Application.DTOs.Nutrition;

namespace GradProject.Application.Interfaces.Nutrition
{
    public interface INutritionTargetsService
    {
        Task<DailyTargetsDto> GetMyDailyTargetsAsync(int userId, CancellationToken ct = default);
    }
}
