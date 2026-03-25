using FluentValidation;
using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces.Gamification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers
{
    [Route("api/v1/badges")]
    [Authorize]
    public class BadgesController : ApiControllerBase
    {
        private readonly IBadgeService _service;
        private readonly IValidator<CreateBadgeRequestDto> _createValidator;
        private readonly IValidator<UpdateBadgeRequestDto> _updateValidator;

        public BadgesController(
            IBadgeService service,
            IValidator<CreateBadgeRequestDto> createValidator,
            IValidator<UpdateBadgeRequestDto> updateValidator)
        {
            _service = service;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<BadgeResponseDto>>> GetAll(CancellationToken ct)
        {
            var items = await _service.GetAllAsync(ct);
            return Ok(items);
        }

        [HttpGet("active")]
        public async Task<ActionResult<IReadOnlyList<BadgeResponseDto>>> GetActive(CancellationToken ct)
        {
            var items = await _service.GetActiveAsync(ct);
            return Ok(items);
        }

        /// <summary>Returns the current user's earned badges (newest first).</summary>
        [HttpGet("me")]
        public async Task<ActionResult<IReadOnlyList<UserBadgeResponseDto>>> GetMyBadges(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var items = await _service.GetUserBadgesAsync(userId, ct);
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<BadgeResponseDto>> GetById(int id, CancellationToken ct)
        {
            var badge = await _service.GetByIdAsync(id, ct);
            return badge == null ? NotFound() : Ok(badge);
        }

        [HttpPost]
        public async Task<ActionResult<BadgeResponseDto>> Create(CreateBadgeRequestDto request, CancellationToken ct)
        {
            var validation = await _createValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            try
            {
                var created = await _service.CreateAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<BadgeResponseDto>> Update(int id, UpdateBadgeRequestDto request, CancellationToken ct)
        {
            var validation = await _updateValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            try
            {
                var updated = await _service.UpdateAsync(id, request, ct);
                return updated == null ? NotFound() : Ok(updated);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _service.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }

    }
}

