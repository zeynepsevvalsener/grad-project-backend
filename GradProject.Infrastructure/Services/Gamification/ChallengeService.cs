using GradProject.Application.Configuration;
using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Exceptions;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GradProject.Infrastructure.Services.Gamification
{
    public class ChallengeService : IChallengeService
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly IOptions<CustomChallengeSettings> _customChallengeSettings;

        public ChallengeService(
            AppDbContext db,
            IMemoryCache cache,
            IOptions<CustomChallengeSettings> customChallengeSettings)
        {
            _db = db;
            _cache = cache;
            _customChallengeSettings = customChallengeSettings;
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
                IsActive = request.IsActive,
                IsCustom = false,
                CreatedByUserId = null,
                CreatedAtUtc = DateTime.UtcNow
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

        public async Task<ChallengeResponseDto> CreateCustomAsync(
            int creatorUserId,
            CreateCustomChallengeRequestDto request,
            CancellationToken ct = default)
        {
            var settings = _customChallengeSettings.Value;
            var now = DateTime.UtcNow;
            var windowStart = now.AddHours(-24);
            var recentCount = await _db.Challenges
                .CountAsync(
                    c => c.IsCustom &&
                         c.CreatedByUserId == creatorUserId &&
                         c.CreatedAtUtc >= windowStart,
                    ct);

            if (recentCount >= settings.MaxCreatesPer24Hours)
            {
                throw new ChallengeCreationRateLimitExceededException(
                    $"You can create at most {settings.MaxCreatesPer24Hours} custom challenges per 24 hours.");
            }

            var start = request.StartDate.HasValue ? NormalizeToUtc(request.StartDate.Value) : now;
            var end = request.EndDate.HasValue
                ? NormalizeToUtc(request.EndDate.Value)
                : start.AddDays(settings.DefaultWindowDays);

            if (end <= start)
                throw new InvalidOperationException("Challenge end must be after start.");

            if (end <= now)
                throw new InvalidOperationException("Challenge end must be in the future.");

            var (type, metric) = request.GoalType switch
            {
                CustomChallengeGoalType.Distance => (ChallengeType.Running, ChallengeMetric.Distance),
                CustomChallengeGoalType.Calories => (ChallengeType.Nutrition, ChallengeMetric.Calories),
                _ => throw new InvalidOperationException("Unsupported goal type.")
            };

            var challenge = new Challenge
            {
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
                Type = type,
                Metric = metric,
                TargetValue = request.TargetValue,
                StartDate = start,
                EndDate = end,
                RewardPoints = 0,
                IsActive = true,
                IsCustom = true,
                CreatedByUserId = creatorUserId,
                CreatedAtUtc = DateTime.UtcNow
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

            var preserveCustomMeta = challenge.IsCustom;
            var preservedCreatedBy = challenge.CreatedByUserId;
            var preservedCreatedAt = challenge.CreatedAtUtc;

            challenge.Title = request.Title;
            challenge.Description = request.Description;
            challenge.Type = request.Type;
            challenge.Metric = request.Metric;
            challenge.TargetValue = request.TargetValue;
            challenge.StartDate = request.StartDate;
            challenge.EndDate = request.EndDate;
            challenge.RewardPoints = request.RewardPoints;
            challenge.IsActive = request.IsActive;

            if (preserveCustomMeta)
            {
                challenge.IsCustom = true;
                challenge.CreatedByUserId = preservedCreatedBy;
                challenge.CreatedAtUtc = preservedCreatedAt;
            }

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

            var existingUserChallenge = await _db.UserChallenges
                .Include(uc => uc.Challenge)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.ChallengeId == challengeId, ct);

            if (existingUserChallenge != null)
                return MapToJoinResponseDto(existingUserChallenge);

            var joinNow = DateTime.UtcNow;
            if (joinNow < challenge.StartDate || joinNow > challenge.EndDate)
                throw new InvalidOperationException("Challenge is not open for joining (outside start/end window).");

            var now = DateTime.UtcNow;
            var userChallenge = new UserChallenge
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

            userChallenge = await _db.UserChallenges
                .Include(uc => uc.Challenge)
                .FirstAsync(uc => uc.Id == userChallenge.Id, ct);

            return MapToJoinResponseDto(userChallenge);
        }

        /// <summary>
        /// API contract: challenge dates are UTC. Unspecified kind is treated as UTC (typical JSON deserialization).
        /// </summary>
        private static DateTime NormalizeToUtc(DateTime value) =>
            value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                _ => value
            };

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
            IsActive = c.IsActive,
            IsCustom = c.IsCustom,
            CreatedByUserId = c.CreatedByUserId,
            CreatedAtUtc = c.CreatedAtUtc
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
                IsActive = challenge.IsActive,
                IsCustom = challenge.IsCustom,
                CreatedByUserId = challenge.CreatedByUserId,
                CreatedAtUtc = challenge.CreatedAtUtc
            };
        }
    }
}

