using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GradProject.Application.DTOs.Common;
using GradProject.Application.Interfaces;
using GradProject.Infrastructure.Services;

namespace GradProject.Api.Controllers
{
    [Route("api/v1/strava")]
    public class StravaController : ApiControllerBase
    {
        private readonly StravaApiService _stravaApiService;
        private readonly IRunActivityService _runActivityService;
        private readonly IStravaService _stravaService;

        public StravaController(
            StravaApiService stravaApiService,
            IRunActivityService runActivityService,
            IStravaService stravaService)
        {
            _stravaApiService = stravaApiService;
            _runActivityService = runActivityService;
            _stravaService = stravaService;
        }

        /// <summary>
        /// SPA / mobile: call with Bearer token, then set <c>window.location.href = authorizeUrl</c>.
        /// (A plain <c>fetch</c> to the redirect-based <see cref="Connect"/> endpoint does not open Strava in the browser.)
        /// </summary>
        [HttpGet("authorize-url")]
        [Authorize]
        public IActionResult GetAuthorizeUrl()
        {
            if (!_stravaApiService.IsOAuthConfigured)
            {
                return StatusCode(503, new
                {
                    message = "Strava OAuth yapılandırılmadı. STRAVA_CLIENT_ID, STRAVA_CLIENT_SECRET ve STRAVA_REDIRECT_URI ortam değişkenlerini ayarlayın.",
                    code = "strava_oauth_not_configured"
                });
            }

            var userId = GetUserIdOrThrow();
            var url = _stravaApiService.GetAuthorizeUrl(userId.ToString());
            return Ok(new { authorizeUrl = url });
        }

        /// <summary>Legacy: full-page redirect when opened in the browser (must include <c>userId</c> query). Prefer <c>authorize-url</c> + JWT from SPAs.</summary>
        [HttpGet("connect")]
        public IActionResult Connect([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("userId zorunlu");

            if (!_stravaApiService.IsOAuthConfigured)
            {
                return StatusCode(503, "Strava OAuth yapılandırılmadı (STRAVA_CLIENT_ID / STRAVA_CLIENT_SECRET / STRAVA_REDIRECT_URI).");
            }

            var url = _stravaApiService.GetAuthorizeUrl(userId);
            return Redirect(url);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                return BadRequest("Eksik parametre");

            if (!int.TryParse(state, out var userId))
                return BadRequest("Geçersiz userId");

            // Exchange code for token
            var tokenResponse = await _stravaApiService.ExchangeCodeForTokenAsync(code);
            if (tokenResponse == null)
                return StatusCode(500, "Token alma hatası");

            if (tokenResponse.ExpiresAt == null || tokenResponse.AthleteId == null)
                return StatusCode(500, "Token response eksik bilgi içeriyor");

            // Save token to User entity via service
            var saved = await _stravaService.SaveStravaTokenAsync(
                userId,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken ?? string.Empty,
                tokenResponse.ExpiresAt.Value,
                tokenResponse.AthleteId.Value,
                ct);

            if (!saved)
                return NotFound("User not found");

            return Ok(new { message = "Strava bağlantısı başarılı", athleteId = tokenResponse.AthleteId });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();

            var accessToken = await _stravaService.GetStravaAccessTokenAsync(userId, ct);
            if (string.IsNullOrWhiteSpace(accessToken))
                return BadRequest("Strava connection is required");

            var result = await _stravaApiService.GetAthleteInfo(accessToken);
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

        [HttpGet("runs")]
        [Authorize]
        public async Task<ActionResult> GetAllRuns(CancellationToken ct = default)
        {
            var userId = GetUserIdOrThrow();
            var recentRuns = await _runActivityService.GetRecentAsync(userId, 100, null, null, ct);
            return Ok(recentRuns);
        }

    }
}
