using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GradProject.Application.DTOs.Running;
using GradProject.Application.Interfaces.Running;

namespace GradProject.Api.Controllers
{
    [Route("api/v1/running/analytics")]
    [Authorize]
    public class RunningAnalyticsController : ApiControllerBase
    {
        private readonly IRunningAnalyticsService _analyticsService;

        public RunningAnalyticsController(IRunningAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet]
        public async Task<ActionResult<RunningAnalyticsResponseDto>> GetAnalytics(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var analytics = await _analyticsService.GetAnalyticsAsync(userId, ct);
            return Ok(analytics);
        }

        [HttpGet("pace-trend")]
        public async Task<ActionResult<PaceTrendResponseDto>> GetPaceTrend(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var paceTrend = await _analyticsService.GetPaceTrendAsync(userId, ct);
            return Ok(paceTrend);
        }

        [HttpGet("heart-rate-trend")]
        public async Task<ActionResult<HeartRateTrendResponseDto>> GetHeartRateTrend(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var heartRateTrend = await _analyticsService.GetHeartRateTrendAsync(userId, ct);
            return Ok(heartRateTrend);
        }

    }
}

