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

