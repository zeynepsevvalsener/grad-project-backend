using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }

        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public UserRole Role { get; set; }
        public string Language { get; set; } = "en";
    }
}
