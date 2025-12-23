using FluentValidation;
using GradProject.Application.DTOs.Common;
using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Nutrition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers
{
    [ApiController]
    [Route("api/v1/foods")]
    [Authorize] // read endpoints also require auth (MVP)
    public class FoodsController : ControllerBase
    {
        private readonly IFoodService _foodService;
        private readonly IValidator<CreateFoodRequestDto> _createValidator;
        private readonly IValidator<UpdateFoodRequestDto> _updateValidator;
        private readonly IFoodSearchService _foodSearchService;


        public FoodsController(
            IFoodService foodService,
            IValidator<CreateFoodRequestDto> createValidator,
            IValidator<UpdateFoodRequestDto> updateValidator, IFoodSearchService foodSearchService)
        {
            _foodService = foodService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _foodSearchService = foodSearchService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<FoodResponseDto>>> GetAll(CancellationToken ct)
        {
            var items = await _foodService.GetAllAsync(ct);
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<FoodResponseDto>> GetById(int id, CancellationToken ct)
        {
            var item = await _foodService.GetByIdAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<FoodResponseDto>> Create(CreateFoodRequestDto request, CancellationToken ct)
        {
            var validation = await _createValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var created = await _foodService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<FoodResponseDto>> Update(int id, UpdateFoodRequestDto request, CancellationToken ct)
        {
            var validation = await _updateValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            try
            {
                var updated = await _foodService.UpdateAsync(id, request, ct);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                // e.g., duplicate name
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _foodService.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpGet("search")]
        public async Task<ActionResult<PagedResultDto<FoodSearchItemDto>>> Search(
             [FromQuery] string? q,
             [FromQuery] int page = 1,
             [FromQuery] int pageSize = 20,
             CancellationToken ct = default)
        {
            var result = await _foodSearchService.SearchAsync(q, page, pageSize, ct);
            return Ok(result);
        }
    }
}
