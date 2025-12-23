using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Profile
{
    public class UpsertMyProfileRequestDto
    {
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public Gender Gender { get; set; } = Gender.Unknown;

        public decimal? Height { get; set; }   // cm
        public decimal? Weight { get; set; }   // kg

        public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.Sedentary;
    }
}
