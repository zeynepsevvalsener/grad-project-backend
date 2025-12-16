using GradProject.Application.DTOs.Auth;
using GradProject.Application.Interfaces;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthService(AppDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == email);
            if (exists)
                throw new InvalidOperationException("Email already exists.");

            var (hash, salt) = _passwordHasher.CreateHash(request.Password);

            var user = new User
            {
                Email = email,
                PasswordHash = hash,
                PasswordSalt = salt,
                // Role default: User
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user);

            return new AuthResponseDto
            {
                AccessToken = token,
                ExpiresAtUtc = expiresAtUtc,
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user is null)
                throw new UnauthorizedAccessException("Invalid credentials.");

            var ok = _passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt);
            if (!ok)
                throw new UnauthorizedAccessException("Invalid credentials.");

            var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user);

            return new AuthResponseDto
            {
                AccessToken = token,
                ExpiresAtUtc = expiresAtUtc,
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role
            };
        }
    }
}
