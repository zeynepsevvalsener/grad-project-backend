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

    /// <summary>
    /// Exposed internally so the test project can drive a single refresh cycle
    /// without triggering the scheduling delay loop.
    /// </summary>
    internal Task RunRefreshForTestAsync(CancellationToken ct) => RunRefreshAsync(ct);

    private async Task RunRefreshAsync(CancellationToken ct)
    {
        _logger.LogInformation("Leaderboard daily refresh started.");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var leaderboardService = scope.ServiceProvider.GetRequiredService<ILeaderboardService>();

        // Step 1: Refresh global leaderboard snapshot. A failure here does not prevent
        // challenge snapshots from running so that partial success is still useful.
        var globalFailed = false;
        try
        {
            await leaderboardService.RefreshGlobalLeaderboardAsync(ct);
            _logger.LogInformation("Global leaderboard snapshot refreshed successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Global leaderboard refresh was cancelled.");
            return;
        }
        catch (Exception ex)
        {
            globalFailed = true;
            _logger.LogError(ex, "Global leaderboard snapshot refresh failed.");
        }

        if (ct.IsCancellationRequested)
            return;

        // Step 2: Refresh per-challenge leaderboard snapshots.
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

        var challengeSuccess = 0;
        var challengeFailed = 0;
        foreach (var challengeId in challengeIds)
        {
            if (ct.IsCancellationRequested)
                break;
            try
            {
                await leaderboardService.RefreshLeaderboardAsync(challengeId, ct);
                challengeSuccess++;
            }
            catch (Exception ex)
            {
                challengeFailed++;
                _logger.LogWarning(ex, "Leaderboard refresh failed for challenge {ChallengeId}.", challengeId);
            }
        }

        if (challengeFailed > 0 || globalFailed)
        {
            _logger.LogWarning(
                "Leaderboard refresh completed with issues — global: {GlobalStatus}, challenges: {ChallengeFailed}/{ChallengeTotal} failed.",
                globalFailed ? "FAILED" : "OK",
                challengeFailed,
                challengeIds.Count);
        }
        else
        {
            _logger.LogInformation(
                "Leaderboard refresh completed successfully — global: OK, challenges: {ChallengeSuccess}/{ChallengeTotal}.",
                challengeSuccess,
                challengeIds.Count);
        }
    }
}
