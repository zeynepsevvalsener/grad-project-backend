using FluentValidation;
using GradProject.Application.DTOs.Auth;
using GradProject.Application.DTOs.Profile;
using GradProject.Application.Interfaces;
using GradProject.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;


namespace GradProject.Api.Controllers
{
    [ApiController]
    [Route("api/v1/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly IValidator<UpsertMyProfileRequestDto> _profileValidator;
        private readonly IJwtTokenService _jwtTokenService;

        public ProfileController(IProfileService profileService, IValidator<UpsertMyProfileRequestDto> profileValidator, IJwtTokenService jwtTokenService)
        {
            _profileService = profileService;
            _profileValidator = profileValidator;
            _jwtTokenService = jwtTokenService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<MyProfileResponseDto>> GetMyProfile(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();

            var profile = await _profileService.GetMyProfileAsync(userId, ct);
            if (profile is null)
                return NotFound(new { message = "Profile not found." });

            return Ok(profile);
        }


        [HttpPut("me")]
        public async Task<ActionResult<MyProfileResponseDto>> UpsertMyProfile([FromBody] UpsertMyProfileRequestDto request, CancellationToken ct)
        {
            var validation = await _profileValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var userId = GetUserIdOrThrow();
            var result = await _profileService.UpsertMyProfileAsync(userId, request, ct);
            return Ok(result);
        }
    


        private int GetUserIdOrThrow()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
                throw new UnauthorizedAccessException("Invalid token.");

            return userId;
        }

        [HttpPut("language")]
        public async Task<ActionResult<TokenRefreshResponseDto>> UpdateMyLanguage(
    [FromBody] UpdateLanguageRequestDto request,
    CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();

            var lang = (request.Language ?? "").Trim().ToLowerInvariant();
            if (lang != "en" && lang != "tr")
                return BadRequest(new { message = "Invalid language. Use 'en' or 'tr'." });

            await _profileService.UpdateMyLanguageAsync(userId, lang, ct);

            // 🔹 DB'den güncel user'ı al
            var user = await HttpContext.RequestServices
                .GetRequiredService<AppDbContext>()
                .Users
                .FirstAsync(u => u.Id == userId, ct);

            // 🔹 Yeni token üret
            var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user);

            return Ok(new TokenRefreshResponseDto
            {
                AccessToken = token,
                ExpiresAtUtc = expiresAtUtc,
                Language = user.Language
            });
        }
    }
}
