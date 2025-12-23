using GradProject.Application.DTOs.Nutrition;

namespace GradProject.Application.Interfaces.Nutrition
{
    public interface IFoodService
    {
        Task<IReadOnlyList<FoodResponseDto>> GetAllAsync(CancellationToken ct = default);
        Task<FoodResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<FoodResponseDto> CreateAsync(CreateFoodRequestDto request, CancellationToken ct = default);
        Task<FoodResponseDto?> UpdateAsync(int id, UpdateFoodRequestDto request, CancellationToken ct = default);

        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
