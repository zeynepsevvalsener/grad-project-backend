using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class ConsumedFoodService : IConsumedFoodService
    {
        private readonly AppDbContext _db;

        public ConsumedFoodService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ConsumedFoodResponseDto>> GetByDateAsync(int userId, DateOnly date, CancellationToken ct = default)
        {
            var start = date.ToDateTime(TimeOnly.MinValue); // local-kind DateTime (we store plain timestamp)
            var end = start.AddDays(1);

            var items = await _db.ConsumedFoods
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.ConsumedAt >= start && x.ConsumedAt < end)
                .OrderBy(x => x.ConsumedAt)
                .Select(x => new ConsumedFoodResponseDto
                {
                    Id = x.Id,
                    FoodId = x.FoodId,
                    FoodName = x.Food.Name,
                    ConsumedAt = x.ConsumedAt,
                    PortionG = x.PortionG
                })
                .ToListAsync(ct);

            return items;
        }

        public async Task<ConsumedFoodResponseDto> CreateAsync(int userId, CreateConsumedFoodRequestDto request, CancellationToken ct = default)
        {
            // Ensure food exists
            var foodExists = await _db.Foods.AnyAsync(f => f.Id == request.FoodId, ct);
            if (!foodExists)
                throw new KeyNotFoundException("Food not found.");

            var entity = new ConsumedFood
            {
                UserId = userId,
                FoodId = request.FoodId,
                PortionG = request.PortionG,
                ConsumedAt = request.ConsumedAt ?? DateTime.UtcNow
            };

            _db.ConsumedFoods.Add(entity);
            await _db.SaveChangesAsync(ct);

            // Get food name for response
            var foodName = await _db.Foods
                .AsNoTracking()
                .Where(f => f.Id == request.FoodId)
                .Select(f => f.Name)
                .FirstAsync(ct);

            return new ConsumedFoodResponseDto
            {
                Id = entity.Id,
                FoodId = entity.FoodId,
                FoodName = foodName,
                ConsumedAt = entity.ConsumedAt,
                PortionG = entity.PortionG
            };
        }

        public async Task<ConsumedFoodResponseDto?> UpdateAsync(int userId, int consumedFoodId, UpdateConsumedFoodRequestDto request, CancellationToken ct = default)
        {
            var entity = await _db.ConsumedFoods
                .Include(x => x.Food)
                .FirstOrDefaultAsync(x => x.Id == consumedFoodId && x.UserId == userId, ct);

            if (entity is null)
                return null;

            entity.PortionG = request.PortionG;
            entity.ConsumedAt = request.ConsumedAt ?? entity.ConsumedAt;

            await _db.SaveChangesAsync(ct);

            return new ConsumedFoodResponseDto
            {
                Id = entity.Id,
                FoodId = entity.FoodId,
                FoodName = entity.Food.Name,
                ConsumedAt = entity.ConsumedAt,
                PortionG = entity.PortionG
            };
        }

        public async Task<bool> DeleteAsync(int userId, int consumedFoodId, CancellationToken ct = default)
        {
            var entity = await _db.ConsumedFoods
                .FirstOrDefaultAsync(x => x.Id == consumedFoodId && x.UserId == userId, ct);

            if (entity is null)
                return false;

            _db.ConsumedFoods.Remove(entity);
            await _db.SaveChangesAsync(ct);

            return true;
        }
    }
}
