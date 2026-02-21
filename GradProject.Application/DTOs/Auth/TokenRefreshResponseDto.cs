namespace GradProject.Application.DTOs.Auth
{
    public class TokenRefreshResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }
        public string Language { get; set; } = "en";
    }
}