using GradProject.Application.DTOs.Running;
using GradProject.Application.Interfaces.Running;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers
{
    /// <summary>
    /// Weekly running aggregates (ISO week, Mon–Sun) derived from synced activities.
    /// </summary>
    [Route("api/v1/running/weekly-summary")]
    [Authorize]
    public class RunningWeeklySummaryController : ApiControllerBase
    {
        private readonly IWeeklyRunningSummaryService _weeklySummaryService;

        public RunningWeeklySummaryController(IWeeklyRunningSummaryService weeklySummaryService)
        {
            _weeklySummaryService = weeklySummaryService;
        }

        /// <summary>
        /// Returns aggregated stats for an ISO week. Omit year and week for the current UTC week.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(WeeklyRunningSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<WeeklyRunningSummaryDto>> GetWeeklySummary(
            [FromQuery] int? year,
            [FromQuery] int? week,
            [FromQuery] bool weekOverWeek = true,
            CancellationToken ct = default)
        {
            var userId = GetUserIdOrThrow();

            try
            {
                var dto = await _weeklySummaryService.GetWeeklySummaryAsync(userId, year, week, weekOverWeek, ct);
                return Ok(dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
