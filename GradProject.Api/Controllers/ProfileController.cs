using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GradProject.Application.DTOs.Profile;
using GradProject.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace GradProject.Api.Controllers
{
    [ApiController]
    [Route("api/v1/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly IValidator<UpsertMyProfileRequestDto> _profileValidator;

        public ProfileController(IProfileService profileService, IValidator<UpsertMyProfileRequestDto> profileValidator)
        {
            _profileService = profileService;
            _profileValidator = profileValidator;
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
    }
}
