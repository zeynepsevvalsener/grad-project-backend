namespace GradProject.Api.Options;

public class LeaderboardRefreshOptions
{
    public const string SectionName = "LeaderboardRefresh";

    public bool Enabled { get; set; } = true;
    public int DailyAtUtcHour { get; set; } = 0;
}
