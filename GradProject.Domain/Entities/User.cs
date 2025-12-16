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

        // 1-1 Profile
        public Profile Profile { get; set; } = null!;
    }
}
