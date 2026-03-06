using System.Text.Json;
using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GradProject.Infrastructure.Services.Gamification;

public class TerritoryClaimDefendService : ITerritoryClaimDefendService
{
    private readonly AppDbContext _db;
    private readonly ITerritoryScoreEngine _scoreEngine;
    private readonly ILogger<TerritoryClaimDefendService> _logger;
    private const int MaxConcurrencyRetries = 2;

    public TerritoryClaimDefendService(
        AppDbContext db,
        ITerritoryScoreEngine scoreEngine,
        ILogger<TerritoryClaimDefendService> logger)
    {
        _db = db;
        _scoreEngine = scoreEngine;
        _logger = logger;
    }

    public async Task<ClaimTerritoryResponseDto> ClaimAsync(
        int userId,
        int runId,
        IReadOnlyList<int> territoryIds,
        CancellationToken ct = default)
    {
        var run = await _db.RunningActivities
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run == null)
            throw new InvalidOperationException("RUN_NOT_FOUND");
        if (run.UserId != userId)
            throw new UnauthorizedAccessException("Run does not belong to user.");

        var contributions = await _scoreEngine.ComputeRunContributionsAsync(runId, territoryIds.ToList(), ct);
        var contributionByTerritory = contributions.ToDictionary(c => c.TerritoryId);

        var rejected = territoryIds
            .Where(id => !contributionByTerritory.ContainsKey(id))
            .Select(id => new RejectedTerritoryDto { TerritoryId = id, Reason = "NOT_COVERED_BY_RUN" })
            .ToList();

        var claimed = new List<int>();
        var updatedOwnership = new List<UpdatedOwnershipDto>();
        var eventsToEmit = new List<TerritoryEventDto>();
        var orderedTerritoryIds = contributionByTerritory.Keys.OrderBy(id => id).ToList();

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            foreach (var territoryId in orderedTerritoryIds)
            {
                var contribution = contributionByTerritory[territoryId];
                var newScore = (decimal)contribution.FinalScore;

                var alreadyProcessed = await _db.TerritoryOwnershipHistories
                    .AnyAsync(h => h.TerritoryId == territoryId && h.ActionRunId == runId, ct);
                if (alreadyProcessed)
                {
                    var existing = await _db.TerritoryOwnershipHistories
                        .Where(h => h.TerritoryId == territoryId && h.ActionRunId == runId)
                        .OrderByDescending(h => h.ActionAt)
                        .FirstOrDefaultAsync(ct);
                    if (existing != null)
                    {
                        claimed.Add(territoryId);
                        updatedOwnership.Add(new UpdatedOwnershipDto
                        {
                            TerritoryId = territoryId,
                            NewOwnerUserId = existing.NewOwnerUserId,
                            PreviousOwnerUserId = existing.PreviousOwnerUserId,
                            ActionType = existing.ActionType.ToString()
                        });
                    }
                    continue;
                }

                var territory = await _db.Territories
                    .FirstOrDefaultAsync(t => t.Id == territoryId, ct);
                if (territory == null)
                {
                    rejected.Add(new RejectedTerritoryDto { TerritoryId = territoryId, Reason = "TERRITORY_NOT_FOUND" });
                    continue;
                }

                var currentSnapshot = territory.CurrentOwnerScoreSnapshot;
                if (territory.CurrentOwnerUserId == null)
                {
                    await ExecuteClaimAsync(territory, userId, runId, newScore, now, claimed, rejected, updatedOwnership, eventsToEmit, contribution.CoverageRatio, contribution.DistanceInTerritory, ct);
                }
                else if (newScore > (currentSnapshot ?? 0))
                {
                    var previousOwnerId = territory.CurrentOwnerUserId.Value;
                    await ExecuteTransferAsync(territory, previousOwnerId, userId, runId, newScore, now, claimed, rejected, updatedOwnership, eventsToEmit, contribution.CoverageRatio, contribution.DistanceInTerritory, ct);
                }
                else
                {
                    rejected.Add(new RejectedTerritoryDto { TerritoryId = territoryId, Reason = "INSUFFICIENT_SCORE" });
                }
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return new ClaimTerritoryResponseDto
        {
            ClaimedTerritories = claimed,
            RejectedTerritories = rejected,
            UpdatedOwnership = updatedOwnership,
            EventsToEmit = eventsToEmit
        };
    }

    public async Task<DefendTerritoryResponseDto> DefendAsync(
        int userId,
        int runId,
        IReadOnlyList<int> territoryIds,
        CancellationToken ct = default)
    {
        var run = await _db.RunningActivities
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run == null)
            throw new InvalidOperationException("RUN_NOT_FOUND");
        if (run.UserId != userId)
            throw new UnauthorizedAccessException("Run does not belong to user.");

        var contributions = await _scoreEngine.ComputeRunContributionsAsync(runId, territoryIds.ToList(), ct);
        var contributionByTerritory = contributions.ToDictionary(c => c.TerritoryId);

        var rejected = territoryIds
            .Where(id => !contributionByTerritory.ContainsKey(id))
            .Select(id => new RejectedTerritoryDto { TerritoryId = id, Reason = "NOT_COVERED_BY_RUN" })
            .ToList();
        var defended = new List<int>();
        var updatedScoreSnapshots = new List<UpdatedScoreSnapshotDto>();
        var eventsToEmit = new List<TerritoryEventDto>();
        var orderedTerritoryIds = contributionByTerritory.Keys.OrderBy(id => id).ToList();

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            foreach (var territoryId in orderedTerritoryIds)
            {
                var contribution = contributionByTerritory[territoryId];
                var territory = await _db.Territories.FirstOrDefaultAsync(t => t.Id == territoryId, ct);
                if (territory == null)
                {
                    rejected.Add(new RejectedTerritoryDto { TerritoryId = territoryId, Reason = "TERRITORY_NOT_FOUND" });
                    continue;
                }
                if (territory.CurrentOwnerUserId != userId)
                {
                    rejected.Add(new RejectedTerritoryDto { TerritoryId = territoryId, Reason = "NOT_OWNER" });
                    continue;
                }

                var alreadyProcessed = await _db.TerritoryOwnershipHistories
                    .AnyAsync(h => h.TerritoryId == territoryId && h.ActionRunId == runId && h.ActionType == OwnershipActionType.Defend, ct);
                if (alreadyProcessed)
                {
                    defended.Add(territoryId);
                    continue;
                }

                var oldScore = (double)(territory.CurrentOwnerScoreSnapshot ?? 0);
                var newScore = (decimal)contribution.FinalScore;
                var defendMetadata = JsonSerializer.Serialize(new { coverageRatio = contribution.CoverageRatio, distanceInTerritory = contribution.DistanceInTerritory });

                var defendSucceeded = false;
                for (var attempt = 0; attempt <= MaxConcurrencyRetries && !defendSucceeded; attempt++)
                {
                    territory.CurrentOwnerScoreSnapshot = newScore;
                    territory.CurrentOwnerSince = now;
                    territory.Version++;

                    _db.TerritoryOwnershipHistories.Add(new TerritoryOwnershipHistory
                    {
                        TerritoryId = territoryId,
                        PreviousOwnerUserId = userId,
                        NewOwnerUserId = userId,
                        ActionType = OwnershipActionType.Defend,
                        ActionRunId = runId,
                        ActionScore = newScore,
                        ActionAt = now,
                        Metadata = defendMetadata
                    });

                    try
                    {
                        await _db.SaveChangesAsync(ct);
                        defended.Add(territoryId);
                        updatedScoreSnapshots.Add(new UpdatedScoreSnapshotDto
                        {
                            TerritoryId = territoryId,
                            OldScore = oldScore,
                            NewScore = contribution.FinalScore
                        });
                        eventsToEmit.Add(new TerritoryEventDto
                        {
                            EventType = "TERRITORY_DEFENDED",
                            UserId = userId,
                            TerritoryId = territoryId,
                            RunId = runId,
                            ActionAt = now,
                            Score = contribution.FinalScore,
                            PreviousOwnerUserId = userId,
                            NewOwnerUserId = userId
                        });
                        defendSucceeded = true;
                    }
                    catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
                    {
                        var pendingHistory = _db.TerritoryOwnershipHistories.Local.FirstOrDefault(h => h.TerritoryId == territoryId && h.ActionRunId == runId);
                        if (pendingHistory != null)
                            _db.TerritoryOwnershipHistories.Remove(pendingHistory);
                        await _db.Entry(territory).ReloadAsync(ct);
                        if (territory.CurrentOwnerUserId != userId)
                        {
                            rejected.Add(new RejectedTerritoryDto { TerritoryId = territoryId, Reason = "NOT_OWNER" });
                            break;
                        }
                    }
                }
                if (!defendSucceeded && !rejected.Any(r => r.TerritoryId == territoryId))
                    _logger.LogWarning("Defend failed for territory {TerritoryId} after retries (concurrency)", territoryId);
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return new DefendTerritoryResponseDto
        {
            DefendedTerritories = defended,
            RejectedTerritories = rejected,
            UpdatedScoreSnapshots = updatedScoreSnapshots,
            EventsToEmit = eventsToEmit
        };
    }

    private async Task ExecuteClaimAsync(
        Territory territory,
        int userId,
        int runId,
        decimal newScore,
        DateTime now,
        List<int> claimed,
        List<RejectedTerritoryDto> rejected,
        List<UpdatedOwnershipDto> updatedOwnership,
        List<TerritoryEventDto> eventsToEmit,
        double coverageRatio,
        double distanceInTerritory,
        CancellationToken ct)
    {
        var metadata = JsonSerializer.Serialize(new { coverageRatio, distanceInTerritory });
        var history = new TerritoryOwnershipHistory
        {
            TerritoryId = territory.Id,
            PreviousOwnerUserId = null,
            NewOwnerUserId = userId,
            ActionType = OwnershipActionType.Claim,
            ActionRunId = runId,
            ActionScore = newScore,
            ActionAt = now,
            Metadata = metadata
        };
        _db.TerritoryOwnershipHistories.Add(history);

        for (var attempt = 0; attempt <= MaxConcurrencyRetries; attempt++)
        {
            territory.CurrentOwnerUserId = userId;
            territory.CurrentOwnerSince = now;
            territory.CurrentOwnerScoreSnapshot = newScore;
            territory.Version++;

            try
            {
                await _db.SaveChangesAsync(ct);
                claimed.Add(territory.Id);
                updatedOwnership.Add(new UpdatedOwnershipDto
                {
                    TerritoryId = territory.Id,
                    NewOwnerUserId = userId,
                    PreviousOwnerUserId = null,
                    ActionType = nameof(OwnershipActionType.Claim)
                });
                eventsToEmit.Add(new TerritoryEventDto
                {
                    EventType = "TERRITORY_CLAIMED",
                    UserId = userId,
                    TerritoryId = territory.Id,
                    RunId = runId,
                    ActionAt = now,
                    Score = (double)newScore,
                    PreviousOwnerUserId = null,
                    NewOwnerUserId = userId
                });
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                await _db.Entry(territory).ReloadAsync(ct);
                if (territory.CurrentOwnerUserId != null)
                {
                    rejected.Add(new RejectedTerritoryDto { TerritoryId = territory.Id, Reason = "CONCURRENCY_LOST" });
                    return;
                }
            }
        }

        _logger.LogWarning("Claim failed for territory {TerritoryId} after retries (concurrency)", territory.Id);
        rejected.Add(new RejectedTerritoryDto { TerritoryId = territory.Id, Reason = "CONCURRENCY_LOST" });
    }

    private async Task ExecuteTransferAsync(
        Territory territory,
        int previousOwnerId,
        int newOwnerId,
        int runId,
        decimal newScore,
        DateTime now,
        List<int> claimed,
        List<RejectedTerritoryDto> rejected,
        List<UpdatedOwnershipDto> updatedOwnership,
        List<TerritoryEventDto> eventsToEmit,
        double coverageRatio,
        double distanceInTerritory,
        CancellationToken ct)
    {
        var metadata = JsonSerializer.Serialize(new { coverageRatio, distanceInTerritory });
        var history = new TerritoryOwnershipHistory
        {
            TerritoryId = territory.Id,
            PreviousOwnerUserId = previousOwnerId,
            NewOwnerUserId = newOwnerId,
            ActionType = OwnershipActionType.Transfer,
            ActionRunId = runId,
            ActionScore = newScore,
            ActionAt = now,
            Metadata = metadata
        };
        _db.TerritoryOwnershipHistories.Add(history);

        for (var attempt = 0; attempt <= MaxConcurrencyRetries; attempt++)
        {
            territory.CurrentOwnerUserId = newOwnerId;
            territory.CurrentOwnerSince = now;
            territory.CurrentOwnerScoreSnapshot = newScore;
            territory.Version++;

            try
            {
                await _db.SaveChangesAsync(ct);
                claimed.Add(territory.Id);
                updatedOwnership.Add(new UpdatedOwnershipDto
                {
                    TerritoryId = territory.Id,
                    NewOwnerUserId = newOwnerId,
                    PreviousOwnerUserId = previousOwnerId,
                    ActionType = nameof(OwnershipActionType.Transfer)
                });
                eventsToEmit.Add(new TerritoryEventDto
                {
                    EventType = "TERRITORY_TRANSFERRED",
                    UserId = newOwnerId,
                    TerritoryId = territory.Id,
                    RunId = runId,
                    ActionAt = now,
                    Score = (double)newScore,
                    PreviousOwnerUserId = previousOwnerId,
                    NewOwnerUserId = newOwnerId
                });
                eventsToEmit.Add(new TerritoryEventDto
                {
                    EventType = "TERRITORY_LOST",
                    UserId = previousOwnerId,
                    TerritoryId = territory.Id,
                    RunId = runId,
                    ActionAt = now,
                    Score = (double)newScore,
                    PreviousOwnerUserId = previousOwnerId,
                    NewOwnerUserId = newOwnerId
                });
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                await _db.Entry(territory).ReloadAsync(ct);
                if (territory.CurrentOwnerUserId != newOwnerId)
                {
                    rejected.Add(new RejectedTerritoryDto { TerritoryId = territory.Id, Reason = "CONCURRENCY_LOST" });
                    return;
                }
            }
        }

        _logger.LogWarning("Transfer failed for territory {TerritoryId} after retries (concurrency)", territory.Id);
        rejected.Add(new RejectedTerritoryDto { TerritoryId = territory.Id, Reason = "CONCURRENCY_LOST" });
    }
}
