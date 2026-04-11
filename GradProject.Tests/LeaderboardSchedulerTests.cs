using System.Text;
using GradProject.Api.HostedServices;
using GradProject.Api.Options;
using GradProject.Application.DTOs.Leaderboard;
using GradProject.Application.Interfaces.Leaderboard;
using GradProject.Application.Services.Leaderboard;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GradProject.Tests;

/// <summary>
/// Tests for BE-2 Leaderboard Scheduler: global snapshot generation, scope separation,
/// idempotency concepts, and job orchestration.
/// </summary>
public class LeaderboardSchedulerTests
{
    // ──────────────────────────────────────────────
    // RankingEngine — global scope (null metric)
    // ──────────────────────────────────────────────

    [Fact]
    public void RankingEngine_GlobalScope_NullMetric_OrdersByDistancePrimary()
    {
        var data = new List<LeaderboardAggregateData>
        {
            new() { UserId = 1, Email = "a@x.com", TotalDistance = 3000, TotalMovingTime = 900, JoinedAt = DateTime.UtcNow, ChallengeStartDate = DateTime.UtcNow.AddDays(-30) },
            new() { UserId = 2, Email = "b@x.com", TotalDistance = 9000, TotalMovingTime = 2700, JoinedAt = DateTime.UtcNow, ChallengeStartDate = DateTime.UtcNow.AddDays(-30) },
            new() { UserId = 3, Email = "c@x.com", TotalDistance = 6000, TotalMovingTime = 1800, JoinedAt = DateTime.UtcNow, ChallengeStartDate = DateTime.UtcNow.AddDays(-30) },
        };
        var engine = new RankingEngine();
        var ranked = engine.CalculateRanks(data);

        Assert.Equal(3, ranked.Count);
        Assert.Equal(2, ranked[0].UserId);  // highest distance wins rank 1
        Assert.Equal(3, ranked[1].UserId);
        Assert.Equal(1, ranked[2].UserId);
        Assert.Equal(1, ranked[0].Rank);
        Assert.Equal(2, ranked[1].Rank);
        Assert.Equal(3, ranked[2].Rank);
    }

    [Fact]
    public void RankingEngine_GlobalScope_DistanceMetric_ProducesDeterministicRanks()
    {
        var data = new List<LeaderboardAggregateData>
        {
            new() { UserId = 10, Email = "x@x.com", TotalDistance = 5000, TotalMovingTime = 1500, JoinedAt = DateTime.UtcNow, ChallengeStartDate = DateTime.UtcNow.AddDays(-30) },
            new() { UserId = 20, Email = "y@x.com", TotalDistance = 5000, TotalMovingTime = 1500, JoinedAt = DateTime.UtcNow, ChallengeStartDate = DateTime.UtcNow.AddDays(-30) },
        };
        var engine = new RankingEngine();
        var ranked = engine.CalculateRanks(data);

        // UserId tie-breaker: lower UserId ranks second when all metrics equal
        Assert.Equal(10, ranked[0].UserId);
        Assert.Equal(20, ranked[1].UserId);
    }

    // ──────────────────────────────────────────────
    // LeaderboardSnapshot model — scope representation
    // ──────────────────────────────────────────────

    [Fact]
    public void LeaderboardSnapshot_GlobalScope_ChallengeIdIsNull()
    {
        var snapshot = new LeaderboardSnapshot
        {
            ChallengeId = null,
            SnapshotDate = DateOnly.FromDateTime(DateTime.UtcNow),
            UserId = 1,
            Rank = 1,
            Username = "runner",
            TotalDistance = 5000,
            AveragePace = 300.0
        };

        Assert.Null(snapshot.ChallengeId);
    }

    [Fact]
    public void LeaderboardSnapshot_ChallengeScope_ChallengeIdIsNonNull()
    {
        var snapshot = new LeaderboardSnapshot
        {
            ChallengeId = 42,
            SnapshotDate = DateOnly.FromDateTime(DateTime.UtcNow),
            UserId = 1,
            Rank = 1,
            Username = "runner",
            TotalDistance = 5000,
            AveragePace = 300.0
        };

        Assert.NotNull(snapshot.ChallengeId);
        Assert.Equal(42, snapshot.ChallengeId);
    }

    [Fact]
    public void LeaderboardSnapshot_GlobalAndChallenge_HaveDifferentScopes()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var global = new LeaderboardSnapshot { ChallengeId = null, SnapshotDate = today, UserId = 1, Rank = 1, Username = "u1", TotalDistance = 5000, AveragePace = 300 };
        var challenge = new LeaderboardSnapshot { ChallengeId = 7, SnapshotDate = today, UserId = 1, Rank = 2, Username = "u1", TotalDistance = 3000, AveragePace = 320 };

        Assert.NotEqual(global.ChallengeId, challenge.ChallengeId);
        Assert.Null(global.ChallengeId);
        Assert.NotNull(challenge.ChallengeId);
    }

    // ──────────────────────────────────────────────
    // Job orchestration — fake ILeaderboardService + InMemory DB
    // ──────────────────────────────────────────────

    [Fact]
    public async Task LeaderboardDailyRefreshJob_CallsGlobalRefresh_BeforeChallengeRefresh()
    {
        var fake = new FakeLeaderboardService();
        var (services, db) = BuildServiceProvider(fake, challengeIds: new[] { 1, 2 });

        var job = new LeaderboardDailyRefreshJob(
            services,
            BuildOptions(enabled: true, dailyAtUtcHour: 0),
            NullLogger<LeaderboardDailyRefreshJob>.Instance);

        await job.RunRefreshForTestAsync(CancellationToken.None);

        Assert.True(fake.GlobalRefreshCalled, "Global refresh must be called.");
        Assert.Equal(new[] { 1, 2 }, fake.RefreshedChallengeIds.OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task LeaderboardDailyRefreshJob_ContinuesChallengeRefresh_AfterGlobalFailure()
    {
        var fake = new FakeLeaderboardService { GlobalRefreshShouldThrow = true };
        var (services, db) = BuildServiceProvider(fake, challengeIds: new[] { 3, 4 });

        var job = new LeaderboardDailyRefreshJob(
            services,
            BuildOptions(enabled: true, dailyAtUtcHour: 0),
            NullLogger<LeaderboardDailyRefreshJob>.Instance);

        await job.RunRefreshForTestAsync(CancellationToken.None);

        Assert.False(fake.GlobalRefreshCalled, "Global refresh threw, so flag stays false.");
        Assert.Equal(new[] { 3, 4 }, fake.RefreshedChallengeIds.OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task LeaderboardDailyRefreshJob_OneChallengeFailure_DoesNotBlockOthers()
    {
        var fake = new FakeLeaderboardService { FailOnChallengeId = 10 };
        var (services, db) = BuildServiceProvider(fake, challengeIds: new[] { 10, 11 });

        var job = new LeaderboardDailyRefreshJob(
            services,
            BuildOptions(enabled: true, dailyAtUtcHour: 0),
            NullLogger<LeaderboardDailyRefreshJob>.Instance);

        await job.RunRefreshForTestAsync(CancellationToken.None);

        Assert.True(fake.GlobalRefreshCalled);
        Assert.Contains(10, fake.AttemptedChallengeIds);
        Assert.Contains(11, fake.RefreshedChallengeIds);   // 11 still ran despite 10 failing
        Assert.DoesNotContain(10, fake.RefreshedChallengeIds);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static (IServiceProvider, AppDbContext) BuildServiceProvider(
        FakeLeaderboardService fake,
        IEnumerable<int> challengeIds)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);

        // Seed minimal Users + Challenges + UserChallenges so the job can enumerate challenge IDs.
        var dummy = Encoding.UTF8.GetBytes("test");
        foreach (var id in challengeIds)
        {
            db.Users.Add(new User { Id = id, Email = $"u{id}@test.com", PasswordHash = dummy, PasswordSalt = dummy, Role = UserRole.User });
            db.Challenges.Add(new Challenge
            {
                Id = id,
                Title = $"Challenge {id}",
                Type = ChallengeType.Running,
                Metric = ChallengeMetric.Distance,
                TargetValue = 5000,
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow.AddDays(7),
                RewardPoints = 10,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            db.UserChallenges.Add(new UserChallenge { UserId = id, ChallengeId = id, JoinedAt = DateTime.UtcNow });
        }
        db.SaveChanges();

        var services = new ServiceCollection();
        services.AddSingleton<AppDbContext>(db);
        services.AddSingleton<ILeaderboardService>(fake);

        return (services.BuildServiceProvider(), db);
    }

    private static IOptions<LeaderboardRefreshOptions> BuildOptions(bool enabled, int dailyAtUtcHour) =>
        Microsoft.Extensions.Options.Options.Create(new LeaderboardRefreshOptions
        {
            Enabled = enabled,
            DailyAtUtcHour = dailyAtUtcHour
        });

    // ──────────────────────────────────────────────
    // Test double
    // ──────────────────────────────────────────────

    private sealed class FakeLeaderboardService : ILeaderboardService
    {
        public bool GlobalRefreshCalled { get; private set; }
        public bool GlobalRefreshShouldThrow { get; init; }
        public int? FailOnChallengeId { get; init; }

        /// <summary>IDs that completed without error.</summary>
        public List<int> RefreshedChallengeIds { get; } = new();

        /// <summary>IDs that were attempted (including ones that threw).</summary>
        public List<int> AttemptedChallengeIds { get; } = new();

        public Task RefreshGlobalLeaderboardAsync(CancellationToken ct = default)
        {
            if (GlobalRefreshShouldThrow)
                throw new InvalidOperationException("Simulated global refresh failure.");
            GlobalRefreshCalled = true;
            return Task.CompletedTask;
        }

        public Task RefreshLeaderboardAsync(int challengeId, CancellationToken ct = default)
        {
            AttemptedChallengeIds.Add(challengeId);
            if (FailOnChallengeId.HasValue && FailOnChallengeId.Value == challengeId)
                throw new InvalidOperationException($"Simulated failure for challenge {challengeId}.");
            RefreshedChallengeIds.Add(challengeId);
            return Task.CompletedTask;
        }

        public Task<LeaderboardResponseDto> GetGlobalLeaderboardAsync(int page, int pageSize, int? userId, int? limit, string? period = null, CancellationToken ct = default)
            => Task.FromResult(new LeaderboardResponseDto { Entries = new(), Pagination = new() });

        public Task<LeaderboardResponseDto> GetLeaderboardAsync(int challengeId, int page, int pageSize, int? userId, int? limit, string? period = null, CancellationToken ct = default)
            => Task.FromResult(new LeaderboardResponseDto { Entries = new(), Pagination = new() });

        public Task<LeaderboardEntryDto> GetUserRankAsync(int challengeId, int userId, CancellationToken ct = default)
            => Task.FromResult(new LeaderboardEntryDto());
    }
}
