using Microsoft.AspNetCore.Mvc;
using GradProject.Api.Services;
using System.Threading.Tasks;

namespace GradProject.Api.Controllers
{
    [ApiController]
    [Route("api/v1/strava/athlete")]
    public class StravaController : ControllerBase
    {
        private readonly StravaApiService _stravaService;

        public StravaController(StravaApiService stravaService)
        {
            _stravaService = stravaService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var result = await _stravaService.GetAthleteInfo();
            return Content(result, "application/json");
        }
    }
}

