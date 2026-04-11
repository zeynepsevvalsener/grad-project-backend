using FluentValidation;
using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Exceptions;
using GradProject.Application.Interfaces.Gamification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers
{
    [Route("api/v1/challenges")]
    [Authorize]
    public class ChallengesController : ApiControllerBase
    {
        private readonly IChallengeService _service;
        private readonly IValidator<CreateChallengeRequestDto> _createValidator;
        private readonly IValidator<CreateCustomChallengeRequestDto> _createCustomValidator;
        private readonly IValidator<UpdateChallengeRequestDto> _updateValidator;

        public ChallengesController(
            IChallengeService service,
            IValidator<CreateChallengeRequestDto> createValidator,
            IValidator<CreateCustomChallengeRequestDto> createCustomValidator,
            IValidator<UpdateChallengeRequestDto> updateValidator)
        {
            _service = service;
            _createValidator = createValidator;
            _createCustomValidator = createCustomValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ChallengeResponseDto>>> GetAll(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var items = await _service.GetAllAsync(userId, ct);
            return Ok(items);
        }

        [HttpGet("active")]
        public async Task<ActionResult<IReadOnlyList<ChallengeResponseDto>>> GetActive(CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var items = await _service.GetActiveAsync(userId, ct);
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ChallengeResponseDto>> GetById(int id, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var challenge = await _service.GetByIdAsync(id, userId, ct);
            return challenge == null ? NotFound() : Ok(challenge);
        }

        [HttpPost("custom")]
        public async Task<ActionResult<ChallengeResponseDto>> CreateCustom(CreateCustomChallengeRequestDto request, CancellationToken ct)
        {
            var validation = await _createCustomValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            try
            {
                var userId = GetUserIdOrThrow();
                var created = await _service.CreateCustomAsync(userId, request, ct);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ChallengeCreationRateLimitExceededException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ChallengeResponseDto>> Create(CreateChallengeRequestDto request, CancellationToken ct)
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
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ChallengeResponseDto>> Update(int id, UpdateChallengeRequestDto request, CancellationToken ct)
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _service.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{challengeId:int}/join")]
        public async Task<ActionResult<JoinChallengeResponseDto>> JoinChallenge(int challengeId, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();

            try
            {
                var result = await _service.JoinChallengeAsync(userId, challengeId, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}