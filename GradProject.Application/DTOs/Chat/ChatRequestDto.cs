namespace GradProject.Application.DTOs.Chat
{
    public class ChatRequestDto
    {
        public string Message { get; set; } = null!;
        public string? SessionId { get; set; }
        public string? Language { get; set; }

        // Hangi context'in toplanacağını AI'dan gelen signal ile belirler
        public string? RequiresContext { get; set; }
    }
}