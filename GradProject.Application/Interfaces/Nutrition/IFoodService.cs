using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.DTOs.Nutrition.Admin;

namespace GradProject.Application.Interfaces.Nutrition
{
    public interface IFoodService
    {
        Task<IReadOnlyList<FoodResponseDto>> GetAllAsync(CancellationToken ct = default);
        Task<FoodResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<FoodResponseDto> CreateAsync(CreateFoodRequestDto request, CancellationToken ct = default);
        Task<FoodResponseDto?> UpdateAsync(int id, UpdateFoodRequestDto request, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

        // Admin
        Task<FoodResponseDto> AdminCreateAsync(AdminCreateFoodRequestDto request, CancellationToken ct = default);
        Task<FoodResponseDto?> AdminUpdateAsync(int id, AdminUpdateFoodRequestDto request, CancellationToken ct = default);
        Task<IReadOnlyList<FoodAliasResponseDto>> GetAliasesAsync(int foodId, CancellationToken ct = default);
        Task AddAliasAsync(int foodId, FoodAliasDto request, CancellationToken ct = default);
        Task<bool> DeleteAliasAsync(int foodId, int aliasId, CancellationToken ct = default);
    }
}