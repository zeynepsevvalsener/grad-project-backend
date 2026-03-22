using System.Diagnostics;
using GradProject.Application.DTOs.Leaderboard;
using GradProject.Application.Interfaces.Leaderboard;
using GradProject.Domain.Entities;
using GradProject.Application.Services.Leaderboard;
using GradProject.Application.Validators.Leaderboard;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GradProject.Infrastructure.Services.Leaderboard;

/// <summary>
/// Service for calculating and retrieving challenge and global leaderboards.
/// Orchestrates validation, aggregation, ranking, and pagination.
///
/// Leaderboard climb detection (BE-5):
///   PersistSnapshotAsync compares new ranks against the most recent PREVIOUS day's snapshot.
///   This means one LeaderboardClimbed event can be emitted per user per scope per day,
///   matching the once-per-day cadence of LeaderboardDailyRefreshJob.
///   If no previous snapshot exists (first-ever refresh), no events are emitted.
///   Manual same-day refreshes use today's snapshot as baseline to avoid repeat events.
/// </summary>
public class LeaderboardService : ILeaderboardService
{
    private readonly AppDbContext _db;
    private readonly ILogger<LeaderboardService> _logger;
    private readonly RankingEngine _rankingEngine;
    private readonly LeaderboardAggregator _aggregator;
    private readonly LeaderboardQueryValidator _validator;
    private readonly ILeaderboardEventPublisher _eventPublisher;

    public LeaderboardService(
        AppDbContext db,
        ILogger<LeaderboardService> logger,
        RankingEngine rankingEngine,
        LeaderboardAggregator aggregator,
        ILeaderboardEventPublisher eventPublisher)
    {
        _db = db;
        _logger = logger;
        _rankingEngine = rankingEngine;
        _aggregator = aggregator;
        _eventPublisher = eventPublisher;
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
                    TerritoryCount = s.TerritoryCount,
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
                var aggregatedData = await _aggregator.GetAggregatedDataAsync(challengeId, null, ct);
                rankedEntries = _rankingEngine.CalculateRanks(aggregatedData);
            }
        }
        else
        {
            var aggregatedData = await _aggregator.GetAggregatedDataAsync(challengeId, dateRange, ct);
            rankedEntries = _rankingEngine.CalculateRanks(aggregatedData);
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
            ChallengeId = challengeId,
            Entries = paginatedEntries,
            CurrentUserEntry = currentUserEntry,
            Pagination = pagination
        };
    }

    /// <inheritdoc />
    public async Task<LeaderboardResponseDto> GetGlobalLeaderboardAsync(
        int page,
        int pageSize,
        int? userId,
        int? limit,
        string? period = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting global leaderboard query: page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
            page, pageSize, userId, limit);

        var query = new LeaderboardQuery
        {
            Page = page,
            PageSize = pageSize,
            Limit = limit,
            UserId = userId
        };

        var validationResult = _validator.Validate(query);
        if (!validationResult.IsValid)
            throw new ArgumentException(validationResult.ErrorMessage);

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

        var aggregatedData = await _aggregator.GetGlobalAggregatedDataAsync(dateRange, ct);
        var rankedEntries = _rankingEngine.CalculateRanks(aggregatedData);

        if (limit.HasValue)
            rankedEntries = rankedEntries.Take(limit.Value).ToList();

        var totalCount = rankedEntries.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        LeaderboardEntryDto? currentUserEntry = null;
        if (userId.HasValue)
            currentUserEntry = rankedEntries.FirstOrDefault(e => e.UserId == userId.Value);

        var paginatedEntries = rankedEntries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var pagination = new PaginationMetadata
        {
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };

        stopwatch.Stop();
        var executionTimeMs = stopwatch.ElapsedMilliseconds;

        if (executionTimeMs > 2000)
        {
            _logger.LogWarning(
                "Slow global leaderboard query: executionTime={ExecutionTimeMs}ms, participantCount={ParticipantCount}, resultCount={ResultCount}",
                executionTimeMs, totalCount, paginatedEntries.Count);
        }
        else
        {
            _logger.LogInformation(
                "Global leaderboard query completed: executionTime={ExecutionTimeMs}ms, participantCount={ParticipantCount}, resultCount={ResultCount}",
                executionTimeMs, totalCount, paginatedEntries.Count);
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
        var challengeExists = await _db.Challenges.AnyAsync(c => c.Id == challengeId, ct);
        if (!challengeExists)
            throw new KeyNotFoundException($"Challenge with ID {challengeId} not found");

        var data = await _aggregator.GetAggregatedDataAsync(challengeId, null, ct);
        var ranked = _rankingEngine.CalculateRanks(data);

        await PersistSnapshotAsync(ranked, challengeId, ct);
    }

    /// <inheritdoc />
    public async Task RefreshGlobalLeaderboardAsync(CancellationToken ct = default)
    {
        var data = await _aggregator.GetGlobalAggregatedDataAsync(ct: ct);
        var ranked = _rankingEngine.CalculateRanks(data);

        await PersistSnapshotAsync(ranked, challengeId: null, ct);
    }

    /// <summary>
    /// Replaces today's snapshot for the given scope (null = global, non-null = challenge).
    /// Idempotent: any prior snapshot for (challengeId, today) is deleted before inserting the new one.
    ///
    /// Rank-climb detection:
    ///   Compares new ranks against the baseline (today's existing snapshot if present, otherwise
    ///   yesterday's). Users whose rank number decreased (moved up the board) emit a
    ///   LeaderboardClimbed achievement event. Publishing errors never block snapshot persistence.
    /// </summary>
    private async Task PersistSnapshotAsync(
        IEnumerable<LeaderboardEntryDto> ranked,
        int? challengeId,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var baselineRanks = await LoadBaselineRanksAsync(challengeId, today, ct);

        if (challengeId.HasValue)
            await _db.LeaderboardSnapshots
                .Where(s => s.ChallengeId == challengeId.Value && s.SnapshotDate == today)
                .ExecuteDeleteAsync(ct);
        else
            await _db.LeaderboardSnapshots
                .Where(s => s.ChallengeId == null && s.SnapshotDate == today)
                .ExecuteDeleteAsync(ct);

        var rankedList = ranked.ToList();

        var snapshots = rankedList.Select(e => new LeaderboardSnapshot
        {
            ChallengeId = challengeId,
            SnapshotDate = today,
            UserId = e.UserId,
            Rank = e.Rank,
            Username = e.Username,
            TerritoryScore = e.TerritoryScore,
            TerritoryCount = e.TerritoryCount,
            TotalDistance = e.TotalDistance,
            AveragePace = e.AveragePace,
            CompletionSpeed = e.CompletionSpeed
        });
        await _db.LeaderboardSnapshots.AddRangeAsync(snapshots, ct);
        await _db.SaveChangesAsync(ct);

        await PublishClimbEventsAsync(rankedList, baselineRanks, challengeId, ct);
    }

    /// <summary>
    /// Loads the most recent rank per user for the given scope from today's or yesterday's snapshot.
    /// Returns an empty dictionary if no prior snapshot exists (first-ever refresh).
    /// </summary>
    private async Task<Dictionary<int, int>> LoadBaselineRanksAsync(
        int? challengeId,
        DateOnly today,
        CancellationToken ct)
    {
        var yesterday = today.AddDays(-1);

        var rows = challengeId.HasValue
            ? await _db.LeaderboardSnapshots
                .Where(s => s.ChallengeId == challengeId.Value &&
                            (s.SnapshotDate == today || s.SnapshotDate == yesterday))
                .Select(s => new { s.UserId, s.Rank, s.SnapshotDate })
                .ToListAsync(ct)
            : await _db.LeaderboardSnapshots
                .Where(s => s.ChallengeId == null &&
                            (s.SnapshotDate == today || s.SnapshotDate == yesterday))
                .Select(s => new { s.UserId, s.Rank, s.SnapshotDate })
                .ToListAsync(ct);

        return rows
            .GroupBy(s => s.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(s => s.SnapshotDate).First().Rank);
    }

    /// <summary>
    /// Emits a LeaderboardClimbed event for each user whose new rank is strictly better than
    /// their baseline. Errors are caught per-user so one failure does not block the rest.
    /// </summary>
    private async Task PublishClimbEventsAsync(
        IReadOnlyList<LeaderboardEntryDto> rankedList,
        Dictionary<int, int> baselineRanks,
        int? challengeId,
        CancellationToken ct)
    {
        foreach (var entry in rankedList)
        {
            if (!baselineRanks.TryGetValue(entry.UserId, out var oldRank) || entry.Rank >= oldRank)
                continue;

            try
            {
                await _eventPublisher.PublishRankChangedAsync(challengeId, entry.UserId, oldRank, entry.Rank, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to publish LeaderboardClimbed event for userId={UserId} scope={Scope}",
                    entry.UserId, challengeId?.ToString() ?? "global");
            }
        }
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

        var challengeExists = await _db.Challenges.AnyAsync(c => c.Id == challengeId, ct);
        if (!challengeExists)
            throw new KeyNotFoundException($"Challenge with ID {challengeId} not found");

        var userParticipates = await _db.UserChallenges
            .AnyAsync(uc => uc.ChallengeId == challengeId && uc.UserId == userId, ct);
        if (!userParticipates)
            throw new KeyNotFoundException($"User {userId} is not participating in challenge {challengeId}");

        var aggregatedData = await _aggregator.GetAggregatedDataAsync(challengeId, null, ct);
        var rankedEntries = _rankingEngine.CalculateRanks(aggregatedData);

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
