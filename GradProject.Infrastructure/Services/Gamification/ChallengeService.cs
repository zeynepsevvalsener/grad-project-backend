using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GradProject.Infrastructure.Services.Gamification
{
    public class ChallengeService : IChallengeService
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        public ChallengeService(AppDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<IReadOnlyList<ChallengeResponseDto>> GetAllAsync(int? userId = null, CancellationToken ct = default)
        {
            var cached = (await _cache.GetOrCreateAsync("challenges:all", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                var challenges = await _db.Challenges
                    .AsNoTracking()
                    .OrderByDescending(c => c.Id)
                    .ToListAsync(ct);
                return challenges.Select(c => MapToResponseDto(c)).ToList();
            }))!;
            return await CloneDtosAndApplyParticipationAsync(cached, userId, ct);
        }

        public async Task<ChallengeResponseDto?> GetByIdAsync(int id, int? userId = null, CancellationToken ct = default)
        {
            var challenge = await _db.Challenges
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (challenge == null)
                return null;

            var dto = MapToResponseDto(challenge);
            if (userId.HasValue)
                await ApplyParticipationAsync(dto, userId.Value, ct);
            return dto;
        }

        public async Task<IReadOnlyList<ChallengeResponseDto>> GetActiveAsync(int? userId = null, CancellationToken ct = default)
        {
            var cached = (await _cache.GetOrCreateAsync("challenges:active", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                var challenges = await _db.Challenges
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.Id)
                    .ToListAsync(ct);
                return challenges.Select(c => MapToResponseDto(c)).ToList();
            }))!;
            return await CloneDtosAndApplyParticipationAsync(cached, userId, ct);
        }

        public async Task<ChallengeResponseDto> CreateAsync(CreateChallengeRequestDto request, CancellationToken ct = default)
        {
            var challenge = new Challenge
            {
                Title = request.Title,
                Description = request.Description,
                Type = request.Type,
                Metric = request.Metric,
                TargetValue = request.TargetValue,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                RewardPoints = request.RewardPoints,
                IsActive = request.IsActive
            };

            _db.Challenges.Add(challenge);
            await _db.SaveChangesAsync(ct);
            _cache.Remove("challenges:all");
            _cache.Remove("challenges:active");

            var createdChallenge = await _db.Challenges
                .AsNoTracking()
                .FirstAsync(c => c.Id == challenge.Id, ct);

            return MapToResponseDto(createdChallenge);
        }

        public async Task<ChallengeResponseDto?> UpdateAsync(int id, UpdateChallengeRequestDto request, CancellationToken ct = default)
        {
            var challenge = await _db.Challenges
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (challenge == null)
                return null;

            challenge.Title = request.Title;
            challenge.Description = request.Description;
            challenge.Type = request.Type;
            challenge.Metric = request.Metric;
            challenge.TargetValue = request.TargetValue;
            challenge.StartDate = request.StartDate;
            challenge.EndDate = request.EndDate;
            challenge.RewardPoints = request.RewardPoints;
            challenge.IsActive = request.IsActive;

            await _db.SaveChangesAsync(ct);
            _cache.Remove("challenges:all");
            _cache.Remove("challenges:active");

            var updatedChallenge = await _db.Challenges
                .AsNoTracking()
                .FirstAsync(c => c.Id == challenge.Id, ct);

            return MapToResponseDto(updatedChallenge);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var challenge = await _db.Challenges
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (challenge == null)
                return false;

            _db.Challenges.Remove(challenge);
            await _db.SaveChangesAsync(ct);
            _cache.Remove("challenges:all");
            _cache.Remove("challenges:active");

            return true;
        }

        public async Task<JoinChallengeResponseDto> JoinChallengeAsync(int userId, int challengeId, CancellationToken ct = default)
        {
            // Load Challenge by challengeId
            var challenge = await _db.Challenges
                .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

            // Validate: Challenge exists
            if (challenge == null)
                throw new KeyNotFoundException("Challenge not found");

            // Validate: IsActive
            if (!challenge.IsActive)
                throw new InvalidOperationException("Challenge is not active");

            // Check if UserChallenge exists
            var existingUserChallenge = await _db.UserChallenges
                .Include(uc => uc.Challenge)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.ChallengeId == challengeId, ct);

            UserChallenge userChallenge;

            if (existingUserChallenge != null)
            {
                // Already joined - return existing record (idempotent)
                userChallenge = existingUserChallenge;
            }
            else
            {
                // Create new UserChallenge
                var now = DateTime.UtcNow;
                userChallenge = new UserChallenge
                {
                    UserId = userId,
                    ChallengeId = challengeId,
                    ProgressDistanceMeters = 0,
                    ProgressCalories = 0,
                    Completed = false,
                    JoinedAt = now
                };

                _db.UserChallenges.Add(userChallenge);
                await _db.SaveChangesAsync(ct);

                // Reload with Challenge navigation
                userChallenge = await _db.UserChallenges
                    .Include(uc => uc.Challenge)
                    .FirstAsync(uc => uc.Id == userChallenge.Id, ct);
            }

            // Map to JoinChallengeResponseDto
            return MapToJoinResponseDto(userChallenge);
        }

        private async Task<IReadOnlyList<ChallengeResponseDto>> CloneDtosAndApplyParticipationAsync(
            List<ChallengeResponseDto> cached,
            int? userId,
            CancellationToken ct)
        {
            var list = cached.Select(CloneChallengeDto).ToList();
            if (!userId.HasValue || list.Count == 0)
                return list;

            var ids = list.Select(c => c.Id).ToList();
            var rows = await _db.UserChallenges
                .AsNoTracking()
                .Where(uc => uc.UserId == userId.Value && ids.Contains(uc.ChallengeId))
                .ToListAsync(ct);

            foreach (var dto in list)
            {
                var uc = rows.FirstOrDefault(r => r.ChallengeId == dto.Id);
                if (uc != null)
                    ApplyParticipationFields(dto, uc);
            }

            return list;
        }

        private async Task ApplyParticipationAsync(ChallengeResponseDto dto, int userId, CancellationToken ct)
        {
            var uc = await _db.UserChallenges
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ChallengeId == dto.Id, ct);
            if (uc != null)
                ApplyParticipationFields(dto, uc);
        }

        private static void ApplyParticipationFields(ChallengeResponseDto dto, UserChallenge uc)
        {
            dto.IsParticipating = true;
            dto.JoinedAt = uc.JoinedAt;
            dto.Completed = uc.Completed;
            dto.CompletedAt = uc.CompletedAt;
            dto.ProgressDistanceMeters = uc.ProgressDistanceMeters;
            dto.ProgressCalories = uc.ProgressCalories;
            dto.ProgressPercent = ComputeProgressPercent(
                dto.Metric, dto.TargetValue, uc.ProgressDistanceMeters, uc.ProgressCalories);
        }

        private static ChallengeResponseDto CloneChallengeDto(ChallengeResponseDto c) => new()
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            Type = c.Type,
            TypeName = c.TypeName,
            Metric = c.Metric,
            MetricName = c.MetricName,
            TargetValue = c.TargetValue,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            RewardPoints = c.RewardPoints,
            IsActive = c.IsActive
        };

        private static double ComputeProgressPercent(
            ChallengeMetric metric,
            double targetValue,
            long progressDistanceMeters,
            int progressCalories)
        {
            if (targetValue <= 0)
                return 0;
            if (metric == ChallengeMetric.Distance)
                return Math.Min(100, progressDistanceMeters / targetValue * 100);
            if (metric == ChallengeMetric.Calories)
                return Math.Min(100, progressCalories / targetValue * 100);
            return 0;
        }

        private static JoinChallengeResponseDto MapToJoinResponseDto(UserChallenge userChallenge)
        {
            var targetValue = userChallenge.Challenge.TargetValue;
            var progressPercent = ComputeProgressPercent(
                userChallenge.Challenge.Metric,
                targetValue,
                userChallenge.ProgressDistanceMeters,
                userChallenge.ProgressCalories);

            return new JoinChallengeResponseDto
            {
                ChallengeId = userChallenge.ChallengeId,
                ProgressDistanceMeters = userChallenge.ProgressDistanceMeters,
                ProgressCalories = userChallenge.ProgressCalories,
                TargetValue = targetValue,
                Completed = userChallenge.Completed,
                JoinedAt = userChallenge.JoinedAt,
                CompletedAt = userChallenge.CompletedAt,
                ProgressPercent = progressPercent
            };
        }

        private static ChallengeResponseDto MapToResponseDto(Challenge challenge)
        {
            var typeName = challenge.Type switch
            {
                ChallengeType.Running => "Running",
                ChallengeType.Nutrition => "Nutrition",
                _ => challenge.Type.ToString()
            };

            var metricName = challenge.Metric switch
            {
                ChallengeMetric.Distance => "Distance",
                ChallengeMetric.Duration => "Duration",
                ChallengeMetric.Pace => "Pace",
                ChallengeMetric.Calories => "Calories",
                ChallengeMetric.Streak => "Streak",
                _ => challenge.Metric.ToString()
            };

            return new ChallengeResponseDto
            {
                Id = challenge.Id,
                Title = challenge.Title,
                Description = challenge.Description,
                Type = challenge.Type,
                TypeName = typeName,
                Metric = challenge.Metric,
                MetricName = metricName,
                TargetValue = challenge.TargetValue,
                StartDate = challenge.StartDate,
                EndDate = challenge.EndDate,
                RewardPoints = challenge.RewardPoints,
                IsActive = challenge.IsActive
            };
        }
    }
}

