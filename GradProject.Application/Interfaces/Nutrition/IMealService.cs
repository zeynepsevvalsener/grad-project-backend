using GradProject.Application.DTOs.Nutrition;

namespace GradProject.Application.Interfaces.Nutrition
{
    public interface IMealService
    {
        Task<IReadOnlyList<MealResponseDto>> GetByDateAsync(int userId, DateOnly date, CancellationToken ct = default);
        Task<MealResponseDto?> GetByIdAsync(int userId, int mealId, CancellationToken ct = default);
        Task<MealResponseDto> CreateAsync(int userId, CreateMealRequestDto request, CancellationToken ct = default);
        Task<MealResponseDto?> UpdateAsync(int userId, int mealId, UpdateMealRequestDto request, CancellationToken ct = default);
        Task<bool> DeleteAsync(int userId, int mealId, CancellationToken ct = default);
    }
}

