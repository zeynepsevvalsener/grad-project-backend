using GradProject.Application.DTOs.Nutrition;

namespace GradProject.Application.Interfaces.Nutrition
{
    public interface IConsumedFoodService
    {
        Task<IReadOnlyList<ConsumedFoodResponseDto>> GetByDateAsync(int userId, DateOnly date, CancellationToken ct = default);

        Task<ConsumedFoodResponseDto> CreateAsync(int userId, CreateConsumedFoodRequestDto request, CancellationToken ct = default);

        Task<ConsumedFoodResponseDto?> UpdateAsync(int userId, int consumedFoodId, UpdateConsumedFoodRequestDto request, CancellationToken ct = default);

        Task<bool> DeleteAsync(int userId, int consumedFoodId, CancellationToken ct = default);
    }
}
