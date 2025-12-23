using GradProject.Application.DTOs.Nutrition;

namespace GradProject.Application.Interfaces.Nutrition
{
    public interface INutritionCalculationService
    {
        Task<TdeeResultDto> GetMyTdeeAsync(int userId, CancellationToken ct = default);
    }
}
