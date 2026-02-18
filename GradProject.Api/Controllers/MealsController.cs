using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentValidation;
using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Nutrition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers
{
    [ApiController]
    [Route("api/v1/meals")]
    [Authorize]
    public class MealsController : ControllerBase
    {
        private readonly IMealService _service;
        private readonly IValidator<CreateMealRequestDto> _createValidator;
        private readonly IValidator<UpdateMealRequestDto> _updateValidator;

        public MealsController(
            IMealService service,
            IValidator<CreateMealRequestDto> createValidator,
            IValidator<UpdateMealRequestDto> updateValidator)
        {
            _service = service;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<MealResponseDto>>> GetByDate(
            [FromQuery] DateOnly? date,
            CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var items = await _service.GetByDateAsync(userId, targetDate, ct);
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MealResponseDto>> GetById(int id, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var meal = await _service.GetByIdAsync(userId, id, ct);
            return meal == null ? NotFound() : Ok(meal);
        }

        [HttpPost]
        public async Task<ActionResult<MealResponseDto>> Create(CreateMealRequestDto request, CancellationToken ct)
        {
            var validation = await _createValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var userId = GetUserIdOrThrow();

            try
            {
                var created = await _service.CreateAsync(userId, request, ct);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<MealResponseDto>> Update(int id, UpdateMealRequestDto request, CancellationToken ct)
        {
            var validation = await _updateValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var userId = GetUserIdOrThrow();

            try
            {
                var updated = await _service.UpdateAsync(userId, id, request, ct);
                return updated == null ? NotFound() : Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var ok = await _service.DeleteAsync(userId, id, ct);
            return ok ? NoContent() : NotFound();
        }

        private int GetUserIdOrThrow()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
                throw new UnauthorizedAccessException("Invalid token.");

            return userId;
        }
    }
}

