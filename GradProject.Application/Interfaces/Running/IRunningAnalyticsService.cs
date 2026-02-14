using GradProject.Application.DTOs.Running;

namespace GradProject.Application.Interfaces.Running
{
    public interface IRunningAnalyticsService
    {
        Task<RunningAnalyticsResponseDto> GetAnalyticsAsync(int userId, CancellationToken ct = default);
        Task<PaceTrendResponseDto> GetPaceTrendAsync(int userId, CancellationToken ct = default);
        Task<HeartRateTrendResponseDto> GetHeartRateTrendAsync(int userId, CancellationToken ct = default);
    }
}

