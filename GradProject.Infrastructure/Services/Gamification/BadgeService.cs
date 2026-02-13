using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Gamification
{
    public class BadgeService : IBadgeService
    {
        private readonly AppDbContext _db;

        public BadgeService(AppDbContext db)
        {
            _db = db;
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

        private static BadgeResponseDto MapToResponseDto(Badge badge)
        {
            var typeName = badge.Type switch
            {
                BadgeType.FirstRun => "First Run",
                BadgeType.Streak => "Streak",
                BadgeType.PersonalBest => "Personal Best",
                BadgeType.Territory => "Territory",
                BadgeType.ChallengeCompletion => "Challenge Completion",
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

