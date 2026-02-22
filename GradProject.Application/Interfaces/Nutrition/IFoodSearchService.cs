using GradProject.Application.DTOs.Common;
using GradProject.Application.DTOs.Nutrition;

namespace GradProject.Application.Interfaces.Nutrition
{
    public interface IFoodSearchService
    {
        Task<PagedResultDto<FoodSearchItemDto>> SearchAsync(
            string? query,
            string? category,
            string? sortBy,
            string? sortDir,
            int page,
            int pageSize,
            CancellationToken ct = default);
    }
}