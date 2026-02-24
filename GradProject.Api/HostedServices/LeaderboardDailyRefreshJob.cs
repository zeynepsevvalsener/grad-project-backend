using GradProject.Api.Options;
using GradProject.Application.Interfaces.Leaderboard;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GradProject.Api.HostedServices;

/// <summary>
/// Runs leaderboard aggregation once per day for challenges that have participants.
/// </summary>
public class LeaderboardDailyRefreshJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LeaderboardRefreshOptions _options;
    private readonly ILogger<LeaderboardDailyRefreshJob> _logger;

    public LeaderboardDailyRefreshJob(
        IServiceProvider serviceProvider,
        IOptions<LeaderboardRefreshOptions> options,
        ILogger<LeaderboardDailyRefreshJob> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        var hour = Math.Clamp(_options.DailyAtUtcHour, 0, 23);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(hour);
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            await RunRefreshAsync(stoppingToken);
        }
    }

    private static TimeSpan GetDelayUntilNextRun(int utcHour)
    {
        var now = DateTime.UtcNow;
        var next = new DateTime(now.Year, now.Month, now.Day, utcHour, 0, 0, DateTimeKind.Utc);
        if (now >= next)
            next = next.AddDays(1);
        return next - now;
    }

    private async Task RunRefreshAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var leaderboardService = scope.ServiceProvider.GetRequiredService<ILeaderboardService>();

        List<int> challengeIds;
        try
        {
            challengeIds = await db.UserChallenges
                .Select(uc => uc.ChallengeId)
                .Distinct()
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load challenge IDs for leaderboard refresh.");
            return;
        }

        var success = 0;
        var failed = 0;
        foreach (var challengeId in challengeIds)
        {
            if (ct.IsCancellationRequested)
                break;
            try
            {
                await leaderboardService.RefreshLeaderboardAsync(challengeId, ct);
                success++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "Leaderboard refresh failed for challenge {ChallengeId}.", challengeId);
            }
        }

        if (failed > 0)
            _logger.LogWarning("Leaderboard refresh completed with {Failed} failures for {Total} challenges.", failed, challengeIds.Count);
    }
}
