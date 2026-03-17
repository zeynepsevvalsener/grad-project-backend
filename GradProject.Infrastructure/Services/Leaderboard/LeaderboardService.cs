using System.Diagnostics;
using GradProject.Application.DTOs.Leaderboard;
using GradProject.Application.Interfaces.Leaderboard;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Application.Services.Leaderboard;
using GradProject.Application.Validators.Leaderboard;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GradProject.Infrastructure.Services.Leaderboard;

/// <summary>
/// Service for calculating and retrieving challenge leaderboards.
/// Orchestrates validation, aggregation, ranking, and pagination.
/// </summary>
public class LeaderboardService : ILeaderboardService
{
    private readonly AppDbContext _db;
    private readonly ILogger<LeaderboardService> _logger;
    private readonly RankingEngine _rankingEngine;
    private readonly LeaderboardAggregator _aggregator;
    private readonly LeaderboardQueryValidator _validator;

    public LeaderboardService(
        AppDbContext db,
        ILogger<LeaderboardService> logger,
        RankingEngine rankingEngine,
        LeaderboardAggregator aggregator)
    {
        _db = db;
        _logger = logger;
        _rankingEngine = rankingEngine;
        _aggregator = aggregator;
        _validator = new LeaderboardQueryValidator();
    }

    /// <summary>
    /// Retrieves paginated leaderboard for a challenge
    /// </summary>
    public async Task<LeaderboardResponseDto> GetLeaderboardAsync(
        int challengeId,
        int page,
        int pageSize,
        int? userId,
        int? limit,
        string? period = null,
        CancellationToken ct = default)
    {
        // Start performance monitoring
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation(
            "Starting leaderboard query for challengeId={ChallengeId}, page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
            challengeId, page, pageSize, userId, limit);

        // Validation phase: Validate query parameters and verify resources exist
        var query = new LeaderboardQuery
        {
            Page = page,
            PageSize = pageSize,
            Limit = limit,
            UserId = userId
        };

        // Validate query parameters (page >= 1, pageSize 1-100, limit >= 1)
        var validationResult = _validator.Validate(query);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(validationResult.ErrorMessage);
        }

        // Verify challenge exists in database
        var challengeExists = await _db.Challenges
            .AnyAsync(c => c.Id == challengeId, ct);
        
        if (!challengeExists)
        {
            throw new KeyNotFoundException($"Challenge with ID {challengeId} not found");
        }

        // If userId provided, verify user is participating in the challenge
        if (userId.HasValue)
        {
            var userParticipates = await _db.UserChallenges
                .AnyAsync(uc => uc.ChallengeId == challengeId && uc.UserId == userId.Value, ct);
            
            if (!userParticipates)
            {
                throw new KeyNotFoundException($"User {userId.Value} is not participating in challenge {challengeId}");
            }
        }

        // Apply default values for pagination
        page = page == 0 ? 1 : page;
        pageSize = pageSize == 0 ? 20 : pageSize;

        (DateTime From, DateTime To)? dateRange = null;
        if (string.Equals(period, "week", StringComparison.OrdinalIgnoreCase))
        {
            var now = DateTime.UtcNow;
            var offset = (int)now.DayOfWeek - (int)DayOfWeek.Monday;
            if (offset < 0) offset += 7;
            var startOfWeek = now.Date.AddDays(-offset);
            dateRange = (startOfWeek, now);
        }
        else if (string.Equals(period, "month", StringComparison.OrdinalIgnoreCase))
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            dateRange = (startOfMonth, now);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        List<LeaderboardEntryDto> rankedEntries;
        if (!dateRange.HasValue)
        {
            var snapshotEntries = await _db.LeaderboardSnapshots
                .Where(s => s.ChallengeId == challengeId && s.SnapshotDate == today)
                .OrderBy(s => s.Rank)
                .Select(s => new LeaderboardEntryDto
                {
                    Rank = s.Rank,
                    UserId = s.UserId,
                    Username = s.Username,
                    TerritoryScore = s.TerritoryScore,
                    TotalDistance = s.TotalDistance,
                    AveragePace = s.AveragePace,
                    CompletionSpeed = s.CompletionSpeed
                })
                .ToListAsync(ct);

            if (snapshotEntries.Count > 0)
            {
                rankedEntries = snapshotEntries;
            }
            else
            {
                var metric = await _db.Challenges
                    .Where(c => c.Id == challengeId)
                    .Select(c => (ChallengeMetric?)c.Metric)
                    .FirstOrDefaultAsync(ct);
                var aggregatedData = await _aggregator.GetAggregatedDataAsync(challengeId, null, ct);
                rankedEntries = _rankingEngine.CalculateRanks(aggregatedData, metric);
            }
        }
        else
        {
            var metric = await _db.Challenges
                .Where(c => c.Id == challengeId)
                .Select(c => (ChallengeMetric?)c.Metric)
                .FirstOrDefaultAsync(ct);
            var aggregatedData = await _aggregator.GetAggregatedDataAsync(challengeId, dateRange, ct);
            rankedEntries = _rankingEngine.CalculateRanks(aggregatedData, metric);
        }

        // Apply limit if provided (for top-N queries)
        if (limit.HasValue)
        {
            rankedEntries = rankedEntries.Take(limit.Value).ToList();
        }

        // Pagination phase: Calculate metadata and extract page subset
        var totalCount = rankedEntries.Count;
        // Calculate total pages: ceiling division ensures partial pages are counted
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Extract current user entry if userId provided (included regardless of pagination)
        LeaderboardEntryDto? currentUserEntry = null;
        if (userId.HasValue)
        {
            currentUserEntry = rankedEntries.FirstOrDefault(e => e.UserId == userId.Value);
        }

        // Apply pagination: Skip to requested page and take pageSize entries
        // Formula: Skip((page - 1) * pageSize) positions the cursor at the start of the page
        var paginatedEntries = rankedEntries
            .Skip((page - 1) * pageSize)  // Skip all entries before current page
            .Take(pageSize)  // Take up to pageSize entries for current page
            .ToList();

        // Build pagination metadata with navigation flags
        var pagination = new PaginationMetadata
        {
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,  // More pages available after current
            HasPreviousPage = page > 1  // Pages available before current
        };

        // Stop performance monitoring and log results
        stopwatch.Stop();
        var executionTimeMs = stopwatch.ElapsedMilliseconds;
        var executionTimeSec = executionTimeMs / 1000.0;

        if (executionTimeSec > 2.0)
        {
            _logger.LogWarning(
                "Slow leaderboard query detected: challengeId={ChallengeId}, executionTime={ExecutionTimeMs}ms, participantCount={ParticipantCount}, resultCount={ResultCount}",
                challengeId, executionTimeMs, totalCount, paginatedEntries.Count);
        }
        else
        {
            _logger.LogInformation(
                "Leaderboard query completed: challengeId={ChallengeId}, executionTime={ExecutionTimeMs}ms, participantCount={ParticipantCount}, resultCount={ResultCount}",
                challengeId, executionTimeMs, totalCount, paginatedEntries.Count);
        }

        return new LeaderboardResponseDto
        {
            Entries = paginatedEntries,
            CurrentUserEntry = currentUserEntry,
            Pagination = pagination
        };
    }

    /// <inheritdoc />
    public async Task RefreshLeaderboardAsync(int challengeId, CancellationToken ct = default)
    {
        var challenge = await _db.Challenges
            .Where(c => c.Id == challengeId)
            .Select(c => new { c.Metric })
            .FirstOrDefaultAsync(ct);
        if (challenge == null)
            throw new KeyNotFoundException($"Challenge with ID {challengeId} not found");

        var data = await _aggregator.GetAggregatedDataAsync(challengeId, null, ct);
        var ranked = _rankingEngine.CalculateRanks(data, challenge.Metric);

        await PersistSnapshotAsync(ranked, challengeId, ct);
    }

    /// <inheritdoc />
    public async Task RefreshGlobalLeaderboardAsync(CancellationToken ct = default)
    {
        var data = await _aggregator.GetGlobalAggregatedDataAsync(ct);
        var ranked = _rankingEngine.CalculateRanks(data, ChallengeMetric.Distance);

        await PersistSnapshotAsync(ranked, challengeId: null, ct);
    }

    /// <summary>
    /// Replaces today's snapshot for the given scope (null = global, non-null = challenge).
    /// Idempotent: any prior snapshot for (challengeId, today) is deleted before inserting the new one.
    /// </summary>
    private async Task PersistSnapshotAsync(
        IEnumerable<LeaderboardEntryDto> ranked,
        int? challengeId,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (challengeId.HasValue)
        {
            await _db.LeaderboardSnapshots
                .Where(s => s.ChallengeId == challengeId.Value && s.SnapshotDate == today)
                .ExecuteDeleteAsync(ct);
        }
        else
        {
            await _db.LeaderboardSnapshots
                .Where(s => s.ChallengeId == null && s.SnapshotDate == today)
                .ExecuteDeleteAsync(ct);
        }

        var snapshots = ranked.Select(e => new LeaderboardSnapshot
        {
            ChallengeId = challengeId,
            SnapshotDate = today,
            UserId = e.UserId,
            Rank = e.Rank,
            Username = e.Username,
            TerritoryScore = e.TerritoryScore,
            TotalDistance = e.TotalDistance,
            AveragePace = e.AveragePace,
            CompletionSpeed = e.CompletionSpeed
        });
        await _db.LeaderboardSnapshots.AddRangeAsync(snapshots, ct);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Calculates individual user rank in a challenge
    /// </summary>
    public async Task<LeaderboardEntryDto> GetUserRankAsync(
        int challengeId,
        int userId,
        CancellationToken ct = default)
    {
        // Start performance monitoring
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation(
            "Starting user rank query for challengeId={ChallengeId}, userId={UserId}",
            challengeId, userId);

        var challenge = await _db.Challenges
            .Where(c => c.Id == challengeId)
            .Select(c => new { c.Metric })
            .FirstOrDefaultAsync(ct);
        if (challenge == null)
            throw new KeyNotFoundException($"Challenge with ID {challengeId} not found");

        var userParticipates = await _db.UserChallenges
            .AnyAsync(uc => uc.ChallengeId == challengeId && uc.UserId == userId, ct);
        if (!userParticipates)
            throw new KeyNotFoundException($"User {userId} is not participating in challenge {challengeId}");

        var aggregatedData = await _aggregator.GetAggregatedDataAsync(challengeId, null, ct);
        var rankedEntries = _rankingEngine.CalculateRanks(aggregatedData, challenge.Metric);

        // Find and return user's entry
        var userEntry = rankedEntries.FirstOrDefault(e => e.UserId == userId);
        
        if (userEntry == null)
        {
            throw new KeyNotFoundException($"User {userId} entry not found in leaderboard");
        }

        // Stop performance monitoring and log results
        stopwatch.Stop();
        var executionTimeMs = stopwatch.ElapsedMilliseconds;
        var executionTimeSec = executionTimeMs / 1000.0;

        if (executionTimeSec > 2.0)
        {
            _logger.LogWarning(
                "Slow user rank query detected: challengeId={ChallengeId}, userId={UserId}, executionTime={ExecutionTimeMs}ms, participantCount={ParticipantCount}",
                challengeId, userId, executionTimeMs, rankedEntries.Count);
        }
        else
        {
            _logger.LogInformation(
                "User rank query completed: challengeId={ChallengeId}, userId={UserId}, executionTime={ExecutionTimeMs}ms, participantCount={ParticipantCount}, userRank={UserRank}",
                challengeId, userId, executionTimeMs, rankedEntries.Count, userEntry.Rank);
        }

        return userEntry;
    }
}
