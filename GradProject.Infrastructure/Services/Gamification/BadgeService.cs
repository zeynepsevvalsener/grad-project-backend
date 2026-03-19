using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GradProject.Infrastructure.Services.Gamification
{
    public class BadgeService : IBadgeService
    {
        private const string PostgresUniqueViolationCode = "23505";
        private readonly AppDbContext _db;
        private readonly ILogger<BadgeService> _logger;

        public BadgeService(AppDbContext db, ILogger<BadgeService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IReadOnlyList<BadgeResponseDto>> GetAllAsync(CancellationToken ct = default)
        {
            var badges = await _db.Badges
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync(ct);

            return badges.Select(b => MapToResponseDto(b)).ToList();
        }

        public async Task<BadgeResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var badge = await _db.Badges
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id, ct);

            return badge == null ? null : MapToResponseDto(badge);
        }

        public async Task<IReadOnlyList<BadgeResponseDto>> GetActiveAsync(CancellationToken ct = default)
        {
            var badges = await _db.Badges
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync(ct);

            return badges.Select(b => MapToResponseDto(b)).ToList();
        }

        public async Task<BadgeResponseDto> CreateAsync(CreateBadgeRequestDto request, CancellationToken ct = default)
        {
            var badge = new Badge
            {
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                IconUrl = request.IconUrl,
                PointsReward = request.PointsReward,
                IsActive = request.IsActive
            };

            _db.Badges.Add(badge);
            await _db.SaveChangesAsync(ct);

            var createdBadge = await _db.Badges
                .AsNoTracking()
                .FirstAsync(b => b.Id == badge.Id, ct);

            return MapToResponseDto(createdBadge);
        }

        public async Task<BadgeResponseDto?> UpdateAsync(int id, UpdateBadgeRequestDto request, CancellationToken ct = default)
        {
            var badge = await _db.Badges
                .FirstOrDefaultAsync(b => b.Id == id, ct);

            if (badge == null)
                return null;

            badge.Name = request.Name;
            badge.Description = request.Description;
            badge.Type = request.Type;
            badge.IconUrl = request.IconUrl;
            badge.PointsReward = request.PointsReward;
            badge.IsActive = request.IsActive;

            await _db.SaveChangesAsync(ct);

            var updatedBadge = await _db.Badges
                .AsNoTracking()
                .FirstAsync(b => b.Id == badge.Id, ct);

            return MapToResponseDto(updatedBadge);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var badge = await _db.Badges
                .FirstOrDefaultAsync(b => b.Id == id, ct);

            if (badge == null)
                return false;

            _db.Badges.Remove(badge);
            await _db.SaveChangesAsync(ct);

            return true;
        }

        public async Task<IReadOnlyList<UserBadgeResponseDto>> GetUserBadgesAsync(int userId, CancellationToken ct = default)
        {
            var exists = await _db.Users.AnyAsync(u => u.Id == userId, ct);
            if (!exists)
                return Array.Empty<UserBadgeResponseDto>();

            var list = await _db.UserBadges
                .AsNoTracking()
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.Badge)
                .OrderByDescending(ub => ub.EarnedAtUtc)
                .ToListAsync(ct);

            return list.Select(ub => MapToUserBadgeResponseDto(ub)).ToList();
        }

        // TODO (BE-3 Badge Engine / notifications): Optional BadgeEvent table for audit/notification pipeline:
        // BadgeEvents (id, user_id, badge_id, event_type, created_at_utc, source, metadata_json). Use when event pipeline is implemented.
        public async Task<AwardBadgeResult> AwardBadgeAsync(int userId, int badgeId, CancellationToken ct = default)
        {
            var userExists = await _db.Users.AnyAsync(u => u.Id == userId, ct);
            if (!userExists)
                return AwardBadgeResult.UserNotFound;

            var badge = await _db.Badges.FirstOrDefaultAsync(b => b.Id == badgeId && b.IsActive, ct);
            if (badge == null)
                return AwardBadgeResult.BadgeNotFound;

            var alreadyEarned = await _db.UserBadges
                .AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId, ct);
            if (alreadyEarned)
                return AwardBadgeResult.AlreadyExists;

            var userBadge = new UserBadge
            {
                UserId = userId,
                BadgeId = badgeId,
                EarnedAtUtc = DateTime.UtcNow
            };
            _db.UserBadges.Add(userBadge);

            try
            {
                await _db.SaveChangesAsync(ct);
                return AwardBadgeResult.Created;
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                return AwardBadgeResult.AlreadyExists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to award badge {BadgeId} to user {UserId}", badgeId, userId);
                return AwardBadgeResult.Failed;
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            var inner = ex.InnerException;
            while (inner != null)
            {
                if (inner is PostgresException pg && pg.SqlState == PostgresUniqueViolationCode)
                    return true;
                inner = inner.InnerException;
            }
            return false;
        }

        private static UserBadgeResponseDto MapToUserBadgeResponseDto(UserBadge ub)
        {
            if (ub.Badge is null)
                throw new InvalidOperationException("Badge must be loaded on UserBadge.");
            var b = ub.Badge;
            return new UserBadgeResponseDto
            {
                BadgeId = b.Id,
                Name = b.Name,
                Description = b.Description,
                Type = b.Type,
                IconUrl = b.IconUrl,
                PointsReward = b.PointsReward,
                EarnedAtUtc = ub.EarnedAtUtc
            };
        }

        private static BadgeResponseDto MapToResponseDto(Badge badge)
        {
            var typeName = badge.Type switch
            {
                BadgeType.FirstRun => "First Run",
                BadgeType.Streak => "Streak",
                BadgeType.PersonalBest => "Personal Best",
                BadgeType.Territory => "Territory",
                BadgeType.ChallengeCompletion => "Challenge Completion",
                BadgeType.TerritoryFirstClaim => "Territory: First Claim",
                BadgeType.TerritoryDefender => "Territory: Defender",
                BadgeType.TerritoryConqueror => "Territory: Conqueror",
                _ => badge.Type.ToString()
            };

            return new BadgeResponseDto
            {
                Id = badge.Id,
                Name = badge.Name,
                Description = badge.Description,
                Type = badge.Type,
                TypeName = typeName,
                IconUrl = badge.IconUrl,
                PointsReward = badge.PointsReward,
                IsActive = badge.IsActive
            };
        }
    }
}


