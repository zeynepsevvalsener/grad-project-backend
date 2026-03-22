using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.DTOs.Nutrition.AI;
using GradProject.Application.Interfaces;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Application.Interfaces.Nutrition.AI;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services.Nutrition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
        private readonly IMealService _mealService;
        private readonly AppDbContext _db;
        private readonly ILocalizationService _localizationService;
        private readonly IWeeklyNutritionReportService _weeklyNutritionReportService;
        public NutritionController(
            INutritionCalculationService nutritionCalculationService,
            INutritionTargetsService nutritionTargetsService,
            IDailyIntakeAggregationService dailyIntakeAggregationService,
            IMealParsingService mealParsingService,
            IMealService mealService,
            ILocalizationService localizationService,
            IWeeklyNutritionReportService weeklyNutritionReportService,
            AppDbContext db)
        {
            _nutritionCalculationService = nutritionCalculationService;
            _nutritionTargetsService = nutritionTargetsService;
            _dailyIntakeAggregationService = dailyIntakeAggregationService;
            _mealParsingService = mealParsingService;
            _mealService = mealService;
            _localizationService = localizationService;
            _weeklyNutritionReportService = weeklyNutritionReportService;
            _db = db;
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
        public async Task<IActionResult> AggregateDailyIntake([FromQuery] string date, CancellationToken ct)
        {
            if (!DateOnly.TryParse(date, out var dateOnly))
            {
                return BadRequest(new { message = "Invalid date format. Use yyyy-MM-dd format." });
            }

            var userId = GetUserIdOrThrow();
            await _dailyIntakeAggregationService.AggregateDailyIntakeAsync(userId, dateOnly, ct);
            return Ok(new { message = $"Daily intake aggregated successfully for {dateOnly:yyyy-MM-dd}" });
        }

        /// <summary>
        /// Gets the daily summary (aggregated intake) for a specific date
        /// </summary>
        [HttpGet("daily-summary")]
        public async Task<ActionResult<DailySummaryDto>> GetDailySummary([FromQuery] string date, CancellationToken ct)
        {
            if (!DateOnly.TryParse(date, out var dateOnly))
                return BadRequest(new { message = "Invalid date format. Use yyyy-MM-dd format." });

            var userId = GetUserIdOrThrow();
            var summary = await _dailyIntakeAggregationService.GetDailySummaryAsync(userId, dateOnly, ct);

            if (summary == null)
                return NotFound(new { message = $"No daily summary found for {dateOnly:yyyy-MM-dd}" });

            // AI feedback — hedef artık summary içinden geliyor, TDEE çağrısı kaldırıldı
            try
            {
                var lang = GetLangFromHeader() ?? "en";
                var payload = new
                {
                    totalIntake = summary.TotalIntakeCalories,
                    targetTdee = summary.CalorieTarget ?? 0,
                    language = lang
                };
                summary.AiFeedback = await _mealParsingService.GetDailyFeedbackAsync(payload, ct);
            }
            catch
            {
                summary.AiFeedback = null;
            }

            return Ok(summary);
        }

        [HttpGet("weekly-report")]
        public async Task<ActionResult<WeeklyNutritionReportDto>> GetWeeklyReport(
    [FromQuery] string weekStart,
    CancellationToken ct)
        {
            if (!DateOnly.TryParse(weekStart, out var weekStartDate))
                return BadRequest(new { message = "Invalid date format. Use yyyy-MM-dd format." });

            var userId = GetUserIdOrThrow();
            var report = await _weeklyNutritionReportService.GetWeeklyReportAsync(userId, weekStartDate, ct);
            return Ok(report);
        }

        /// <summary>
        /// Rule-based NLP parsing: converts meal text into structured food items + portion grams + macro estimates.
        /// </summary>
        [HttpPost("parse-meal")]
        public async Task<ActionResult<MealResponseDto>> ParseAndSaveMeal(
    [FromBody] MealParseRequestDto request, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();

            // Dil çözümleme
            if (string.IsNullOrWhiteSpace(request.Language) || request.Language == "string")
            {
                var userLang = await _db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => u.Language)
                    .FirstOrDefaultAsync(ct);

                request.Language = !string.IsNullOrWhiteSpace(userLang)
                    ? userLang
                    : GetLangFromHeader() ?? "en";
            }

            var parseResult = await _mealParsingService.ParseAsync(userId, request, ct);

            var validItems = parseResult.Items
                .Where(i => i.MatchedFoodId.HasValue)
                .ToList();

            if (!validItems.Any())
                return BadRequest(new { message = _localizationService.Get("ai.food.notfound") });

            var mealFoods = validItems.Select(item => new MealFoodDto
            {
                FoodId = item.MatchedFoodId!.Value,
                Quantity = item.PortionG,
                Unit = "g"
            }).ToList();

            var consumedAt = request.ConsumedAt ?? DateTime.UtcNow;

            // AI'dan MealType geldiyse onu kullan, gelmezse saate göre belirle
            var mealType = request.MealType ?? DetermineMealTypeByTime(consumedAt);

            var createMealRequest = new CreateMealRequestDto
            {
                LoggedAt = consumedAt,
                MealType = mealType,
                RawText = request.Text,
                Notes = "AI Auto-Parsed",
                Foods = mealFoods
            };

            var savedMeal = await _mealService.CreateAsync(userId, createMealRequest, ct);
            savedMeal.Feedback = parseResult.Feedback;

            foreach (var food in savedMeal.Foods)
            {
                var alias = await _db.FoodAliases
                    .AsNoTracking()
                    .Where(a => a.FoodId == food.FoodId && a.Language == request.Language)
                    .Select(a => a.Alias)
                    .FirstOrDefaultAsync(ct);

                if (!string.IsNullOrWhiteSpace(alias))
                    food.FoodName = alias;
            }

            return Ok(savedMeal);
        }

        private static Domain.Enums.MealType DetermineMealTypeByTime(DateTime time)
        {
            var hour = time.Hour;

            if (hour >= 5 && hour < 11) return Domain.Enums.MealType.BREAKFAST;
            if (hour >= 11 && hour < 16) return Domain.Enums.MealType.LUNCH;
            if (hour >= 16 && hour < 22) return Domain.Enums.MealType.DINNER;

            return Domain.Enums.MealType.SNACK;
        }

        private int GetUserIdOrThrow()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
                throw new UnauthorizedAccessException("Invalid token.");

            return userId;
        }

        private string? GetLangFromHeader()
        {
            if (!Request.Headers.TryGetValue("Accept-Language", out var values))
                return null;

            var raw = values.ToString();
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            // "tr-TR,tr;q=0.9,en;q=0.8" -> "tr"
            var first = raw.Split(',')[0].Trim();
            if (string.IsNullOrWhiteSpace(first))
                return null;

            var lang = first.Split('-')[0].Trim().ToLowerInvariant();
            return string.IsNullOrWhiteSpace(lang) ? null : lang;
        }
    }
}