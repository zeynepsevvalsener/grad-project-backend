using GradProject.Application.DTOs.Profile;

namespace GradProject.Application.Interfaces
{
    public interface IProfileService
    {
        Task<MyProfileResponseDto?> GetMyProfileAsync(int userId, CancellationToken ct = default);
        Task<MyProfileResponseDto> UpsertMyProfileAsync(int userId, UpsertMyProfileRequestDto request, CancellationToken ct = default);
        Task UpdateMyLanguageAsync(int userId, string language, CancellationToken ct = default);
    }
}
