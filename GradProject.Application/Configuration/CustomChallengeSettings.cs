namespace GradProject.Application.Configuration
{
    public class CustomChallengeSettings
    {
        public const string SectionName = "CustomChallenges";

        /// <summary>When end date is omitted, challenge length is start + this many days (default start = UtcNow).</summary>
        public int DefaultWindowDays { get; set; } = 30;

        public double MaxTargetDistanceMeters { get; set; } = 5_000_000;

        public double MaxTargetCalories { get; set; } = 500_000;

        public int MaxCreatesPer24Hours { get; set; } = 10;
    }
}
