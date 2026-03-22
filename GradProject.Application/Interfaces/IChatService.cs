using GradProject.Application.DTOs.Chat;

namespace GradProject.Application.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponseDto> SendMessageAsync(
            int userId,
            ChatRequestDto request,
            CancellationToken ct = default);
    }
}