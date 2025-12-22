using GradProject.Domain.Enums;

namespace GradProject.Domain.Entities
{
    public class Profile
    {
        public int Id { get; set; }

        // 1-1 ilişki için ZORUNLU
        public int UserId { get; set; }

        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public Gender Gender { get; set; } = Gender.Unknown;
        public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.Sedentary;


        public decimal? Height { get; set; }   // cm
        public decimal? Weight { get; set; }   // kg

        // Navigation
        public User User { get; set; } = null!;
    }
}
