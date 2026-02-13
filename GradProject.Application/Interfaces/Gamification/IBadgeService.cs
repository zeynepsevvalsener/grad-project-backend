using GradProject.Application.DTOs.Gamification;

namespace GradProject.Application.Interfaces.Gamification
{
    public interface IBadgeService
    {
        Task<IReadOnlyList<BadgeResponseDto>> GetAllAsync(CancellationToken ct = default);
        Task<BadgeResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<BadgeResponseDto>> GetActiveAsync(CancellationToken ct = default);
        Task<BadgeResponseDto> CreateAsync(CreateBadgeRequestDto request, CancellationToken ct = default);
        Task<BadgeResponseDto?> UpdateAsync(int id, UpdateBadgeRequestDto request, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}

