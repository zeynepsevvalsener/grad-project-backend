using GradProject.Application.DTOs.Gamification;

namespace GradProject.Application.Interfaces.Gamification
{
    public interface IChallengeService
    {
        Task<IReadOnlyList<ChallengeResponseDto>> GetAllAsync(CancellationToken ct = default);
        Task<ChallengeResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<ChallengeResponseDto>> GetActiveAsync(CancellationToken ct = default);
        Task<ChallengeResponseDto> CreateAsync(CreateChallengeRequestDto request, CancellationToken ct = default);
        Task<ChallengeResponseDto?> UpdateAsync(int id, UpdateChallengeRequestDto request, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}

