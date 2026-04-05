using FluentValidation;
using GradProject.Application.DTOs.Common;
using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.DTOs.Nutrition.Admin;
using GradProject.Application.Interfaces.Nutrition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers
{
    [Route("api/v1/foods")]
    [Authorize]
    public class FoodsController : ApiControllerBase
    {
        private readonly IFoodService _foodService;
        private readonly IValidator<CreateFoodRequestDto> _createValidator;
        private readonly IValidator<UpdateFoodRequestDto> _updateValidator;
        private readonly IFoodSearchService _foodSearchService;

        public FoodsController(
            IFoodService foodService,
            IValidator<CreateFoodRequestDto> createValidator,
            IValidator<UpdateFoodRequestDto> updateValidator,
            IFoodSearchService foodSearchService)
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

        [HttpGet("search")]
        public async Task<ActionResult<PagedResultDto<FoodSearchItemDto>>> Search(
            [FromQuery] string? q,
            [FromQuery] string? category,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortDir,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _foodSearchService.SearchAsync(q, category, sortBy, sortDir, page, pageSize, ct);
            return Ok(result);
        }

        // Admin

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<FoodResponseDto>> Create(AdminCreateFoodRequestDto request, CancellationToken ct)
        {
            try
            {
                var created = await _foodService.AdminCreateAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<FoodResponseDto>> Update(int id, AdminUpdateFoodRequestDto request, CancellationToken ct)
        {
            try
            {
                var updated = await _foodService.AdminUpdateAsync(id, request, ct);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
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

    }
}