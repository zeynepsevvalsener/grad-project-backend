namespace GradProject.Application.DTOs.Chat
{
    public class ChatResponseDto
    {
        public string SessionId { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string State { get; set; } = null!;
        public object? Data { get; set; }
        public string? RequiresAction { get; set; }
        public string? RequiresContext { get; set; }
    }
}