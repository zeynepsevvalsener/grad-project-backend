using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class FoodService : IFoodService
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        public FoodService(AppDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<IReadOnlyList<FoodResponseDto>> GetAllAsync(CancellationToken ct = default)
        {
            return (await _cache.GetOrCreateAsync("foods:all", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                var foods = await _db.Foods
                .AsNoTracking()
                    .OrderBy(f => f.Name)
                    .ToListAsync(ct);
                return foods.Select(MapToDto).ToList();
            }))!;
        }

        public async Task<FoodResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var food = await _db.Foods
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id, ct);

            return food is null ? null : MapToDto(food);
        }

        public async Task<FoodResponseDto> CreateAsync(CreateFoodRequestDto request, CancellationToken ct = default)
        {
            var name = request.Name.Trim();

            var exists = await _db.Foods.AnyAsync(f => f.Name == name, ct);
            if (exists)
                throw new InvalidOperationException("Food with the same name already exists.");

            var entity = new Food
            {
                Name = name,
                Category = request.Category.Trim(),
                Source = request.Source.Trim(),

                Kcal = request.Kcal,
                ProteinG = request.ProteinG,
                FatG = request.FatG,
                CarbG = request.CarbG,
                SugarG = request.SugarG,
                FiberG = request.FiberG,
                SodiumMg = request.SodiumMg,
                DefaultPortionG = request.DefaultPortionG,

                
            };

            _db.Foods.Add(entity);
            await _db.SaveChangesAsync(ct);
            _cache.Remove("foods:all");

            return MapToDto(entity);
        }

        public async Task<FoodResponseDto?> UpdateAsync(int id, UpdateFoodRequestDto request, CancellationToken ct = default)
        {
            var entity = await _db.Foods.FirstOrDefaultAsync(f => f.Id == id, ct);
            if (entity is null)
                return null;

            var newName = request.Name.Trim();

            // if name changes, ensure uniqueness
            if (!string.Equals(entity.Name, newName, StringComparison.Ordinal))
            {
                var exists = await _db.Foods.AnyAsync(f => f.Id != id && f.Name == newName, ct);
                if (exists)
                    throw new InvalidOperationException("Food with the same name already exists.");
            }

            entity.Name = newName;
            entity.Category = request.Category.Trim();
            entity.Source = request.Source.Trim();

            entity.Kcal = request.Kcal;
            entity.ProteinG = request.ProteinG;
            entity.FatG = request.FatG;
            entity.CarbG = request.CarbG;
            entity.SugarG = request.SugarG;
            entity.FiberG = request.FiberG;
            entity.SodiumMg = request.SodiumMg;
            entity.DefaultPortionG = request.DefaultPortionG;

            

            await _db.SaveChangesAsync(ct);
            _cache.Remove("foods:all");

            return MapToDto(entity);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.Foods.FirstOrDefaultAsync(f => f.Id == id, ct);
            if (entity is null)
                return false;

            _db.Foods.Remove(entity);
            await _db.SaveChangesAsync(ct);
            _cache.Remove("foods:all");

            return true;
        }

        private static FoodResponseDto MapToDto(Food f) => new()
        {
            Id = f.Id,
            Name = f.Name,
            Category = f.Category,
            Source = f.Source,

            Kcal = f.Kcal,
            ProteinG = f.ProteinG,
            FatG = f.FatG,
            CarbG = f.CarbG,
            SugarG = f.SugarG,
            FiberG = f.FiberG,
            SodiumMg = f.SodiumMg,

            DefaultPortionG = f.DefaultPortionG,
           
        };
    }
}
