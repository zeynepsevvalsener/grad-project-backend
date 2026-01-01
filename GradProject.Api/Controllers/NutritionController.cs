using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.DTOs.Nutrition.AI;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Application.Interfaces.Nutrition.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers
{
    [ApiController]
    [Route("api/v1/nutrition")]
    [Authorize]
    public class NutritionController : ControllerBase
    {
        private readonly INutritionCalculationService _nutritionCalculationService;
        private readonly INutritionTargetsService _nutritionTargetsService;
        private readonly IDailyIntakeAggregationService _dailyIntakeAggregationService;
        private readonly IMealParsingService _mealParsingService;
        private readonly IMealParsingService _mealParsingService;

        public NutritionController(
        INutritionCalculationService nutritionCalculationService,
        INutritionTargetsService nutritionTargetsService,
        IDailyIntakeAggregationService dailyIntakeAggregationService,
        IMealParsingService mealParsingService)
        {
            _nutritionCalculationService = nutritionCalculationService;
            _nutritionTargetsService = nutritionTargetsService;
            _dailyIntakeAggregationService = dailyIntakeAggregationService;
            _mealParsingService = mealParsingService;
        }


        [HttpGet("tdee")]
        public async Task<ActionResult<TdeeResultDto>> GetMyTdee(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var result = await _nutritionCalculationService.GetMyTdeeAsync(userId, ct);
            return Ok(result);
        }

        [HttpGet("targets")]
        public async Task<ActionResult<DailyTargetsDto>> GetMyDailyTargets(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var result = await _nutritionTargetsService.GetMyDailyTargetsAsync(userId, ct);
            return Ok(result);
        }

        /// <summary>
        /// Aggregates daily intake for a specific date and updates/creates DailySummary
        /// </summary>
        [HttpPost("aggregate-intake")]
        public async Task<IActionResult> AggregateDailyIntake([FromQuery] DateOnly date, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            await _dailyIntakeAggregationService.AggregateDailyIntakeAsync(userId, date, ct);
            return Ok(new { message = $"Daily intake aggregated successfully for {date:yyyy-MM-dd}" });
        }

        /// <summary>
        /// Gets the daily summary (aggregated intake) for a specific date
        /// </summary>
        [HttpGet("daily-summary")]
        public async Task<ActionResult<DailySummaryDto>> GetDailySummary([FromQuery] DateOnly date, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var summary = await _dailyIntakeAggregationService.GetDailySummaryAsync(userId, date, ct);
            return summary == null
                ? NotFound(new { message = $"No daily summary found for {date:yyyy-MM-dd}" })
                : Ok(summary);
        }

        /// <summary>
        /// Rule-based NLP parsing: converts meal text into structured food items + portion grams + macro estimates.
        /// </summary>
        [HttpPost("parse-meal")]
        public async Task<ActionResult<MealParseResultDto>> ParseMeal([FromBody] MealParseRequestDto request, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var result = await _mealParsingService.ParseAsync(userId, request, ct);
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
