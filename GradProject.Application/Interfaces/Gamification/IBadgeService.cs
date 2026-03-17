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

        /// <summary>Returns badges earned by the user, newest first.</summary>
        Task<IReadOnlyList<UserBadgeResponseDto>> GetUserBadgesAsync(int userId, CancellationToken ct = default);

        /// <summary>Awards a badge to the user if not already earned; idempotent. Validates user and badge existence.</summary>
        Task<AwardBadgeResult> AwardBadgeAsync(int userId, int badgeId, CancellationToken ct = default);
    }
}

