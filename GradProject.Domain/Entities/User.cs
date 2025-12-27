using GradProject.Domain.Enums;

namespace GradProject.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; } = null!;

        public byte[] PasswordHash { get; set; } = null!;
        public byte[] PasswordSalt { get; set; } = null!;

        public UserRole Role { get; set; } = UserRole.User;

        // Strava Connection
        public string? StravaAccessToken { get; set; }
        public string? StravaRefreshToken { get; set; }
        public DateTime? StravaTokenExpiresAt { get; set; }
        public long? StravaAthleteId { get; set; }
        public DateTime? StravaConnectedAt { get; set; }

        // 1-1 Profile
        public Profile? Profile { get; set; }
    }
}
