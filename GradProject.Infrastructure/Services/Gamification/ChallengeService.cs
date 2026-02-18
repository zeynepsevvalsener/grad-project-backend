using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Gamification
{
    public class ChallengeService : IChallengeService
    {
        private readonly AppDbContext _db;

        public ChallengeService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ChallengeResponseDto>> GetAllAsync(CancellationToken ct = default)
        {
            var challenges = await _db.Challenges
                .AsNoTracking()
                .OrderByDescending(c => c.Id)
                .ToListAsync(ct);

            return challenges.Select(c => MapToResponseDto(c)).ToList();
        }

        public async Task<ChallengeResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var challenge = await _db.Challenges
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            return challenge == null ? null : MapToResponseDto(challenge);
        }

        public async Task<IReadOnlyList<ChallengeResponseDto>> GetActiveAsync(CancellationToken ct = default)
        {
            var challenges = await _db.Challenges
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.Id)
                .ToListAsync(ct);

            return challenges.Select(c => MapToResponseDto(c)).ToList();
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

        private static JoinChallengeResponseDto MapToJoinResponseDto(UserChallenge userChallenge)
        {
            var targetValue = userChallenge.Challenge.TargetValue;
            double progressPercent = 0;

            // Calculate progress percent based on challenge metric
            if (userChallenge.Challenge.Metric == Domain.Enums.ChallengeMetric.Distance)
            {
                progressPercent = targetValue > 0
                    ? Math.Min(100, (userChallenge.ProgressDistanceMeters / targetValue) * 100)
                    : 0;
            }
            else if (userChallenge.Challenge.Metric == Domain.Enums.ChallengeMetric.Calories)
            {
                progressPercent = targetValue > 0
                    ? Math.Min(100, (userChallenge.ProgressCalories / targetValue) * 100)
                    : 0;
            }

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

