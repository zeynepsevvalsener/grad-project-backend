using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Profile
{
    public class MyProfileResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; }

        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }

        public ActivityLevel ActivityLevel { get; set; }
    }
}
