using GradProject.Application.DTOs.Profile;
using GradProject.Application.Interfaces;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services
{
    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _db;

        public ProfileService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<MyProfileResponseDto?> GetMyProfileAsync(int userId, CancellationToken ct = default)
        {
            var profile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (profile is null) return null;

            return new MyProfileResponseDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                DateOfBirth = profile.DateOfBirth,
                Gender = profile.Gender,
                Height = profile.Height,
                Weight = profile.Weight,
                ActivityLevel = profile.ActivityLevel
            };
        }

        public async Task<MyProfileResponseDto> UpsertMyProfileAsync(int userId, UpsertMyProfileRequestDto request, CancellationToken ct = default)
        {
            // ensure user exists (optional but nice)
            var userExists = await _db.Users.AnyAsync(u => u.Id == userId, ct);
            if (!userExists)
                throw new UnauthorizedAccessException("Invalid user.");

            var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile is null)
            {
                profile = new Profile
                {
                    UserId = userId
                };
                _db.Profiles.Add(profile);
            }

            profile.FirstName = request.FirstName.Trim();
            profile.LastName = request.LastName?.Trim();
            profile.DateOfBirth = request.DateOfBirth;
            profile.Gender = request.Gender;
            profile.Height = request.Height;
            profile.Weight = request.Weight;
            profile.ActivityLevel = request.ActivityLevel;

            await _db.SaveChangesAsync(ct);

            return new MyProfileResponseDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                DateOfBirth = profile.DateOfBirth,
                Gender = profile.Gender,
                Height = profile.Height,
                Weight = profile.Weight,
                ActivityLevel = profile.ActivityLevel
            };
        }
        public async Task UpdateMyLanguageAsync(int userId, string language, CancellationToken ct = default)
        {
            // user exists + update language
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null)
                throw new UnauthorizedAccessException("Invalid user.");

            // no-op if same
            if (string.Equals(user.Language, language, StringComparison.OrdinalIgnoreCase))
                return;

            user.Language = language;
            await _db.SaveChangesAsync(ct);
        }
    }
}
