using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GradProject.Application.Interfaces;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace GradProject.Api.Controllers
{
    [ApiController]
    [Route("api/v1/strava")]
    public class StravaController : ControllerBase
    {
        private readonly StravaApiService _stravaService;
        private readonly IRunActivityService _runActivityService;
        private readonly AppDbContext _db;

        public StravaController(StravaApiService stravaService, IRunActivityService runActivityService, AppDbContext db)
        {
            _stravaService = stravaService;
            _runActivityService = runActivityService;
            _db = db;
        }

        [HttpGet("connect")]
        public IActionResult Connect([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("userId zorunlu");

            var url = _stravaService.GetAuthorizeUrl(userId);
            return Redirect(url);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                return BadRequest("Eksik parametre");

            if (!int.TryParse(state, out var userId))
                return BadRequest("Geçersiz userId");

            // Exchange code for token
            var tokenResponse = await _stravaService.ExchangeCodeForTokenAsync(code);
            if (tokenResponse == null)
                return StatusCode(500, "Token alma hatası");

            // Save token to User entity
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found");

            user.StravaAccessToken = tokenResponse.AccessToken;
            user.StravaRefreshToken = tokenResponse.RefreshToken;
            user.StravaTokenExpiresAt = tokenResponse.ExpiresAt;
            user.StravaAthleteId = tokenResponse.AthleteId;
            user.StravaConnectedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Strava bağlantısı başarılı", athleteId = tokenResponse.AthleteId });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);
            
            if (user == null)
                return NotFound("User not found");
            
            if (string.IsNullOrWhiteSpace(user.StravaAccessToken))
                return BadRequest("Strava connection is required");
            
            var result = await _stravaService.GetAthleteInfo(user.StravaAccessToken);
            return Content(result, "application/json");
        }

        [HttpGet("runs/latest")]
        [Authorize]
        public async Task<IActionResult> FetchLatestRun(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var result = await _runActivityService.FetchLatestStravaRunAsync(userId, ct);

            if (!result.Success)
            {
                if (result.RequiresStravaConnection)
                {
                    return BadRequest(new { message = result.ErrorMessage, requiresStravaConnection = true });
                }
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(result.RunActivity);
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
