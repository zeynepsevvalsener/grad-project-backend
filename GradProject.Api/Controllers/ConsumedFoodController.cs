using System.Security.Claims;
using FluentValidation;
using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Nutrition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;


namespace GradProject.Api.Controllers
{
    [ApiController]
    [Route("api/v1/consumed-food")]
    [Authorize]
    public class ConsumedFoodController : ControllerBase
    {
        private readonly IConsumedFoodService _service;
        private readonly IValidator<CreateConsumedFoodRequestDto> _createValidator;
        private readonly IValidator<UpdateConsumedFoodRequestDto> _updateValidator;

        public ConsumedFoodController(
            IConsumedFoodService service,
            IValidator<CreateConsumedFoodRequestDto> createValidator,
            IValidator<UpdateConsumedFoodRequestDto> updateValidator)
        {
            _service = service;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ConsumedFoodResponseDto>>> GetByDate(
            [FromQuery] DateOnly date,
            CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var items = await _service.GetByDateAsync(userId, date, ct);
            return Ok(items);
        }

        [HttpPost]
        public async Task<ActionResult<ConsumedFoodResponseDto>> Create(CreateConsumedFoodRequestDto request, CancellationToken ct)
        {
            var validation = await _createValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var userId = GetUserIdOrThrow();

            try
            {
                var created = await _service.CreateAsync(userId, request, ct);
                return Ok(created);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ConsumedFoodResponseDto>> Update(int id, UpdateConsumedFoodRequestDto request, CancellationToken ct)
        {
            var validation = await _updateValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var userId = GetUserIdOrThrow();
            var updated = await _service.UpdateAsync(userId, id, request, ct);

            // owner check is inside service (id + userId)
            return updated is null ? NotFound() : Ok(updated);
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
            // We store user id in JWT "sub" claim
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
                throw new UnauthorizedAccessException("Invalid token.");

            return userId;
        }
    }
}
