using GradProject.Application.DTOs.Leaderboard;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Leaderboard;

/// <summary>
/// Aggregates leaderboard data from database using a single query.
/// Supports both challenge-scoped and global (platform-wide) aggregation.
/// </summary>
public class LeaderboardAggregator
{
    private readonly AppDbContext _db;

    public LeaderboardAggregator(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves platform-wide aggregated leaderboard data for all users who have running activity.
    /// No challenge filter is applied; all recorded running activities are included.
    /// TerritoryScore defaults to 0 (no per-user territory aggregation at the global scope yet).
    /// CompletionSpeed is not applicable globally and is left as null.
    /// </summary>
    public async Task<List<LeaderboardAggregateData>> GetGlobalAggregatedDataAsync(
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT u."Id" AS "UserId", u."Email",
                   p."FirstName", p."LastName",
                   NULL::float8 AS "TerritoryScore",
                   NULL::bigint AS "TotalDurationSeconds",
                   NULL::timestamptz AS "CompletedAt",
                   MIN(ra."StartTime") AS "JoinedAt",
                   '0001-01-01'::timestamptz AS "ChallengeStartDate",
                   COALESCE(SUM(ra."DistanceMeters"), 0)::float8 AS "TotalDistance",
                   COALESCE(SUM(ra."MovingTimeSeconds"), 0)::int4 AS "TotalMovingTime"
            FROM "Users" u
            JOIN "RunningActivities" ra ON ra."UserId" = u."Id"
            LEFT JOIN "Profiles" p ON p."UserId" = u."Id"
            GROUP BY u."Id", u."Email", p."FirstName", p."LastName"
            """;

        return await _db.Database
            .SqlQueryRaw<LeaderboardAggregateData>(sql)
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
                       uc."TerritoryScore", uc."TotalDurationSeconds", uc."CompletedAt",
                       uc."JoinedAt", c."StartDate" AS "ChallengeStartDate",
                       COALESCE(agg."Distance", 0)::float8 AS "TotalDistance",
                       COALESCE(agg."MovingTime", 0)::int4 AS "TotalMovingTime"
                FROM "UserChallenges" uc
                JOIN "Users" u ON uc."UserId" = u."Id"
                JOIN "Challenges" c ON uc."ChallengeId" = c."Id"
                LEFT JOIN "Profiles" p ON u."Id" = p."UserId"
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
                       uc."TerritoryScore", uc."TotalDurationSeconds", uc."CompletedAt",
                       uc."JoinedAt", c."StartDate" AS "ChallengeStartDate",
                       COALESCE(agg."Distance", 0)::float8 AS "TotalDistance",
                       COALESCE(agg."MovingTime", 0)::int4 AS "TotalMovingTime"
                FROM "UserChallenges" uc
                JOIN "Users" u ON uc."UserId" = u."Id"
                JOIN "Challenges" c ON uc."ChallengeId" = c."Id"
                LEFT JOIN "Profiles" p ON u."Id" = p."UserId"
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
            args = new object[] { challengeId, challengeId, challengeId };
        }

        var results = await _db.Database
            .SqlQueryRaw<LeaderboardAggregateData>(sql, args)
            .ToListAsync(ct);

        return results;
    }
}
