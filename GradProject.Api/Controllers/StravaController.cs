using Microsoft.AspNetCore.Mvc;
using GradProject.Api.Services;
using System.Threading.Tasks;

namespace GradProject.Api.Controllers
{
    [ApiController]
    [Route("api/v1/strava")]
    public class StravaController : ControllerBase
    {
        private readonly StravaApiService _stravaService;

        public StravaController(StravaApiService stravaService)
        {
            _stravaService = stravaService;
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

            var token = await _stravaService.ExchangeCodeForTokenAsync(code, state);
            if (token.StartsWith("Token alma hatası"))
                return StatusCode(500, token);

            // Test amaçlı hemen veri çekip bağlantının başarılı olduğunu bildir
            var athleteInfo = await _stravaService.GetAthleteInfo(state);
            if (athleteInfo.StartsWith("Hata"))
                return StatusCode(500, "Token alındı, ama api isteği başarısız: " + athleteInfo);

            return Ok("Strava bağlantısı başarılı, veri: " + athleteInfo);
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("userId zorunlu");
            var result = await _stravaService.GetAthleteInfo(userId);
            return Content(result, "application/json");
        }
    }
}
