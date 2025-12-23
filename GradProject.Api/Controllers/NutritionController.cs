using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Nutrition;
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


        public NutritionController(INutritionCalculationService nutritionCalculationService,
                INutritionTargetsService nutritionTargetsService)
        {
            _nutritionCalculationService = nutritionCalculationService;
            _nutritionTargetsService = nutritionTargetsService;
        }


        [HttpGet("tdee")]
        public async Task<ActionResult<TdeeResultDto>> GetMyTdee(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var result = await _nutritionCalculationService.GetMyTdeeAsync(userId, ct);
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

        [HttpGet("targets")]
        public async Task<ActionResult<DailyTargetsDto>> GetMyDailyTargets(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var result = await _nutritionTargetsService.GetMyDailyTargetsAsync(userId, ct);
            return Ok(result);
        }

    }
}
