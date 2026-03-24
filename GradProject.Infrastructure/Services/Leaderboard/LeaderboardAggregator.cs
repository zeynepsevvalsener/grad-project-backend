using GradProject.Application.DTOs.Leaderboard;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Leaderboard;

/// <summary>
/// Aggregates leaderboard data from database using raw SQL queries.
/// Supports both challenge-scoped and global aggregation.
/// </summary>
public class LeaderboardAggregator
{
    private readonly AppDbContext _db;

    public LeaderboardAggregator(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves global aggregated leaderboard data across all challenges and running activities.
    /// Eligible users: anyone with at least one UserChallenge or one RunningActivity.
    /// Territory score = SUM(ActionScore) from territory_ownership_history for Claim/Defend/Transfer,
    /// joined to runs (same time window as distance when a range is applied).
    /// Territory count = COUNT of active Territories where user is CurrentOwnerUserId (same for global and challenge boards).
    /// Distance/moving time = SUM of all RunningActivities (optionally filtered by date range).
    /// CompletedAt is always null (no single challenge), so CompletionSpeed will be null.
    /// </summary>
    public async Task<List<LeaderboardAggregateData>> GetGlobalAggregatedDataAsync(
        (DateTime From, DateTime To)? dateRange = null,
        CancellationToken ct = default)
    {
        string sql;
        object[] args;

        if (dateRange.HasValue)
        {
            var (from, to) = dateRange.Value;
            sql = """
                SELECT eligible."UserId",
                       u."Email",
                       p."FirstName",
                       p."LastName",
                       COALESCE(tp."TerritoryScore", 0)::float8 AS "TerritoryScore",
                       COALESCE(tc."TerritoryCount", 0)::int4 AS "TerritoryCount",
                       NULL::bigint AS "TotalDurationSeconds",
                       NULL::timestamp AS "CompletedAt",
                       COALESCE(join_meta."EarliestJoin", NOW()) AS "JoinedAt",
                       COALESCE(join_meta."EarliestJoin", NOW()) AS "ChallengeStartDate",
                       COALESCE(ra_agg."Distance", 0)::float8 AS "TotalDistance",
                       COALESCE(ra_agg."MovingTime", 0)::int4 AS "TotalMovingTime"
                FROM (
                    SELECT DISTINCT "UserId" FROM "UserChallenges"
                    UNION
                    SELECT DISTINCT "UserId" FROM "RunningActivities"
                ) eligible
                JOIN "Users" u ON eligible."UserId" = u."Id"
                LEFT JOIN "Profiles" p ON u."Id" = p."UserId"
                LEFT JOIN (
                    SELECT t."CurrentOwnerUserId" AS "UserId",
                           COUNT(*)::int4 AS "TerritoryCount"
                    FROM "Territories" t
                    WHERE t."IsActive" = TRUE AND t."CurrentOwnerUserId" IS NOT NULL
                    GROUP BY t."CurrentOwnerUserId"
                ) tc ON eligible."UserId" = tc."UserId"
                LEFT JOIN (
                    SELECT uc."UserId",
                           MIN(uc."JoinedAt") AS "EarliestJoin"
                    FROM "UserChallenges" uc
                    GROUP BY uc."UserId"
                ) join_meta ON eligible."UserId" = join_meta."UserId"
                LEFT JOIN (
                    SELECT h."NewOwnerUserId" AS "UserId",
                           SUM(CAST(h."ActionScore" AS double precision)) AS "TerritoryScore"
                    FROM "territory_ownership_history" h
                    INNER JOIN "RunningActivities" ra ON ra."Id" = h."ActionRunId"
                    WHERE h."ActionType" IN (0, 1, 3)
                      AND ra."StartTime" >= {0} AND ra."StartTime" <= {1}
                    GROUP BY h."NewOwnerUserId"
                ) tp ON eligible."UserId" = tp."UserId"
                LEFT JOIN (
                    SELECT ra."UserId",
                           SUM(ra."DistanceMeters") AS "Distance",
                           SUM(ra."MovingTimeSeconds") AS "MovingTime"
                    FROM "RunningActivities" ra
                    WHERE ra."StartTime" >= {0} AND ra."StartTime" <= {1}
                    GROUP BY ra."UserId"
                ) ra_agg ON eligible."UserId" = ra_agg."UserId"
                """;
            args = new object[] { from, to };
        }
        else
        {
            sql = """
                SELECT eligible."UserId",
                       u."Email",
                       p."FirstName",
                       p."LastName",
                       COALESCE(tp."TerritoryScore", 0)::float8 AS "TerritoryScore",
                       COALESCE(tc."TerritoryCount", 0)::int4 AS "TerritoryCount",
                       NULL::bigint AS "TotalDurationSeconds",
                       NULL::timestamp AS "CompletedAt",
                       COALESCE(join_meta."EarliestJoin", NOW()) AS "JoinedAt",
                       COALESCE(join_meta."EarliestJoin", NOW()) AS "ChallengeStartDate",
                       COALESCE(ra_agg."Distance", 0)::float8 AS "TotalDistance",
                       COALESCE(ra_agg."MovingTime", 0)::int4 AS "TotalMovingTime"
                FROM (
                    SELECT DISTINCT "UserId" FROM "UserChallenges"
                    UNION
                    SELECT DISTINCT "UserId" FROM "RunningActivities"
                ) eligible
                JOIN "Users" u ON eligible."UserId" = u."Id"
                LEFT JOIN "Profiles" p ON u."Id" = p."UserId"
                LEFT JOIN (
                    SELECT t."CurrentOwnerUserId" AS "UserId",
                           COUNT(*)::int4 AS "TerritoryCount"
                    FROM "Territories" t
                    WHERE t."IsActive" = TRUE AND t."CurrentOwnerUserId" IS NOT NULL
                    GROUP BY t."CurrentOwnerUserId"
                ) tc ON eligible."UserId" = tc."UserId"
                LEFT JOIN (
                    SELECT uc."UserId",
                           MIN(uc."JoinedAt") AS "EarliestJoin"
                    FROM "UserChallenges" uc
                    GROUP BY uc."UserId"
                ) join_meta ON eligible."UserId" = join_meta."UserId"
                LEFT JOIN (
                    SELECT h."NewOwnerUserId" AS "UserId",
                           SUM(CAST(h."ActionScore" AS double precision)) AS "TerritoryScore"
                    FROM "territory_ownership_history" h
                    INNER JOIN "RunningActivities" ra ON ra."Id" = h."ActionRunId"
                    WHERE h."ActionType" IN (0, 1, 3)
                    GROUP BY h."NewOwnerUserId"
                ) tp ON eligible."UserId" = tp."UserId"
                LEFT JOIN (
                    SELECT ra."UserId",
                           SUM(ra."DistanceMeters") AS "Distance",
                           SUM(ra."MovingTimeSeconds") AS "MovingTime"
                    FROM "RunningActivities" ra
                    GROUP BY ra."UserId"
                ) ra_agg ON eligible."UserId" = ra_agg."UserId"
                """;
            args = Array.Empty<object>();
        }

        return await _db.Database
            .SqlQueryRaw<LeaderboardAggregateData>(sql, args)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves aggregated leaderboard data for a specific challenge.
    /// Executes a single database query with GROUP BY on UserChallenge,
    /// joining with Users, Profiles, Challenge, and RunningActivities.
    /// Aggregates SUM(DistanceMeters) and SUM(MovingTimeSeconds) from activities
    /// within the challenge date range.
    /// </summary>
    /// <param name="challengeId">The challenge identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of aggregated leaderboard data for all participants</returns>
    public async Task<List<LeaderboardAggregateData>> GetAggregatedDataAsync(
        int challengeId,
        (DateTime From, DateTime To)? dateRange = null,
        CancellationToken ct = default)
    {
        string sql;
        object[] args;
        if (dateRange.HasValue)
        {
            var (from, to) = dateRange.Value;
            sql = """
                SELECT uc."UserId", u."Email", p."FirstName", p."LastName",
                       COALESCE(tp."TerritoryScore", 0)::float8 AS "TerritoryScore",
                       COALESCE(tc."TerritoryCount", 0)::int4 AS "TerritoryCount",
                       uc."TotalDurationSeconds", uc."CompletedAt",
                       uc."JoinedAt", c."StartDate" AS "ChallengeStartDate",
                       COALESCE(agg."Distance", 0)::float8 AS "TotalDistance",
                       COALESCE(agg."MovingTime", 0)::int4 AS "TotalMovingTime"
                FROM "UserChallenges" uc
                JOIN "Users" u ON uc."UserId" = u."Id"
                JOIN "Challenges" c ON uc."ChallengeId" = c."Id"
                LEFT JOIN "Profiles" p ON u."Id" = p."UserId"
                LEFT JOIN (
                    SELECT t."CurrentOwnerUserId" AS "UserId",
                           COUNT(*)::int4 AS "TerritoryCount"
                    FROM "Territories" t
                    WHERE t."IsActive" = TRUE AND t."CurrentOwnerUserId" IS NOT NULL
                    GROUP BY t."CurrentOwnerUserId"
                ) tc ON uc."UserId" = tc."UserId"
                LEFT JOIN (
                    SELECT h."NewOwnerUserId" AS "UserId",
                           SUM(CAST(h."ActionScore" AS double precision)) AS "TerritoryScore"
                    FROM "territory_ownership_history" h
                    INNER JOIN "RunningActivities" ra ON ra."Id" = h."ActionRunId"
                    WHERE h."ActionType" IN (0, 1, 3)
                      AND ra."StartTime" >= {0} AND ra."StartTime" <= {1}
                    GROUP BY h."NewOwnerUserId"
                ) tp ON uc."UserId" = tp."UserId"
                LEFT JOIN (
                    SELECT ra."UserId",
                           SUM(ra."DistanceMeters") AS "Distance",
                           SUM(ra."MovingTimeSeconds") AS "MovingTime"
                    FROM "RunningActivities" ra
                    WHERE ra."StartTime" >= {0} AND ra."StartTime" <= {1}
                    GROUP BY ra."UserId"
                ) agg ON uc."UserId" = agg."UserId"
                WHERE uc."ChallengeId" = {2}
                """;
            args = new object[] { from, to, challengeId };
        }
        else
        {
            sql = """
                SELECT uc."UserId", u."Email", p."FirstName", p."LastName",
                       COALESCE(tp."TerritoryScore", 0)::float8 AS "TerritoryScore",
                       COALESCE(tc."TerritoryCount", 0)::int4 AS "TerritoryCount",
                       uc."TotalDurationSeconds", uc."CompletedAt",
                       uc."JoinedAt", c."StartDate" AS "ChallengeStartDate",
                       COALESCE(agg."Distance", 0)::float8 AS "TotalDistance",
                       COALESCE(agg."MovingTime", 0)::int4 AS "TotalMovingTime"
                FROM "UserChallenges" uc
                JOIN "Users" u ON uc."UserId" = u."Id"
                JOIN "Challenges" c ON uc."ChallengeId" = c."Id"
                LEFT JOIN "Profiles" p ON u."Id" = p."UserId"
                LEFT JOIN (
                    SELECT t."CurrentOwnerUserId" AS "UserId",
                           COUNT(*)::int4 AS "TerritoryCount"
                    FROM "Territories" t
                    WHERE t."IsActive" = TRUE AND t."CurrentOwnerUserId" IS NOT NULL
                    GROUP BY t."CurrentOwnerUserId"
                ) tc ON uc."UserId" = tc."UserId"
                LEFT JOIN (
                    SELECT h."NewOwnerUserId" AS "UserId",
                           SUM(CAST(h."ActionScore" AS double precision)) AS "TerritoryScore"
                    FROM "territory_ownership_history" h
                    INNER JOIN "RunningActivities" ra ON ra."Id" = h."ActionRunId"
                    WHERE h."ActionType" IN (0, 1, 3)
                      AND ra."StartTime" >= (SELECT "StartDate" FROM "Challenges" WHERE "Id" = {0})
                      AND ra."StartTime" <= (SELECT "EndDate" FROM "Challenges" WHERE "Id" = {0})
                    GROUP BY h."NewOwnerUserId"
                ) tp ON uc."UserId" = tp."UserId"
                LEFT JOIN (
                    SELECT ra."UserId",
                           SUM(ra."DistanceMeters") AS "Distance",
                           SUM(ra."MovingTimeSeconds") AS "MovingTime"
                    FROM "RunningActivities" ra
                    WHERE ra."StartTime" >= (SELECT "StartDate" FROM "Challenges" WHERE "Id" = {0})
                      AND ra."StartTime" <= (SELECT "EndDate" FROM "Challenges" WHERE "Id" = {0})
                    GROUP BY ra."UserId"
                ) agg ON uc."UserId" = agg."UserId"
                WHERE uc."ChallengeId" = {0}
                """;
            args = new object[] { challengeId };
        }

        var results = await _db.Database
            .SqlQueryRaw<LeaderboardAggregateData>(sql, args)
            .ToListAsync(ct);

        return results;
    }
}
