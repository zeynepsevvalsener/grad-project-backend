using GradProject.Application.Interfaces;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GradProject.Infrastructure.Services
{
    /// <summary>
    /// Service implementation for managing Strava integration and user connections.
    /// </summary>
    public class StravaService : IStravaService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<StravaService> _logger;

        public StravaService(AppDbContext db, ILogger<StravaService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<bool> SaveStravaTokenAsync(
            int userId,
            string accessToken,
            string refreshToken,
            DateTime expiresAt,
            long athleteId,
            CancellationToken ct = default)
        {
            try
            {
                var user = await _db.Users.FindAsync(new object[] { userId }, ct);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for Strava token save", userId);
                    return false;
                }

                user.StravaAccessToken = accessToken;
                user.StravaRefreshToken = refreshToken;
                user.StravaTokenExpiresAt = expiresAt;
                user.StravaAthleteId = athleteId;
                user.StravaConnectedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Strava token saved for user {UserId}, athlete {AthleteId}", userId, athleteId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Strava token for user {UserId}", userId);
                return false;
            }
        }

        public async Task<string?> GetStravaAccessTokenAsync(int userId, CancellationToken ct = default)
        {
            try
            {
                var user = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId, ct);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for Strava token retrieval", userId);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(user.StravaAccessToken))
                {
                    _logger.LogInformation("User {UserId} has no Strava connection", userId);
                    return null;
                }

                return user.StravaAccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Strava token for user {UserId}", userId);
                return null;
            }
        }
    }
}

