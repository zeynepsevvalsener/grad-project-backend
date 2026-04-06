using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.DTOs.Nutrition.Admin;
using GradProject.Application.Interfaces;
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
        private readonly ICurrentLanguage _currentLanguage;

        public FoodService(AppDbContext db, IMemoryCache cache, ICurrentLanguage currentLanguage)
        {
            _db = db;
            _cache = cache;
            _currentLanguage = currentLanguage;
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

            if (food is null) return null;

            var lang = (_currentLanguage.Value ?? "en").Substring(0, 2).ToLowerInvariant();
            var alias = await _db.FoodAliases
                .AsNoTracking()
                .Where(a => a.FoodId == id && a.Language == lang)
                .Select(a => a.Alias)
                .FirstOrDefaultAsync(ct);

            var dto = MapToDto(food);
            dto.DisplayName = alias ?? food.Name;
            return dto;
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
            if (entity is null) return null;

            var newName = request.Name.Trim();

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
            if (entity is null) return false;

            _db.Foods.Remove(entity);
            await _db.SaveChangesAsync(ct);
            _cache.Remove("foods:all");

            return true;
        }

        // Admin

        public async Task<FoodResponseDto> AdminCreateAsync(AdminCreateFoodRequestDto request, CancellationToken ct = default)
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

            foreach (var a in request.Aliases)
            {
                _db.FoodAliases.Add(new FoodAlias
                {
                    FoodId = entity.Id,
                    Language = a.Language,
                    Alias = a.Alias,
                    NormalizedAlias = a.Alias.Trim().ToLowerInvariant(),
                });
            }

            if (request.Aliases.Any())
                await _db.SaveChangesAsync(ct);

            _cache.Remove("foods:all");

            return MapToDto(entity);
        }

        public async Task<FoodResponseDto?> AdminUpdateAsync(int id, AdminUpdateFoodRequestDto request, CancellationToken ct = default)
        {
            var entity = await _db.Foods.FirstOrDefaultAsync(f => f.Id == id, ct);
            if (entity is null) return null;

            if (request.Name is not null)
            {
                var newName = request.Name.Trim();
                if (!string.Equals(entity.Name, newName, StringComparison.Ordinal))
                {
                    var exists = await _db.Foods.AnyAsync(f => f.Id != id && f.Name == newName, ct);
                    if (exists)
                        throw new InvalidOperationException("Food with the same name already exists.");
                }
                entity.Name = newName;
            }

            if (request.Category is not null) entity.Category = request.Category.Trim();
            if (request.Source is not null) entity.Source = request.Source.Trim();
            if (request.Kcal is not null) entity.Kcal = request.Kcal.Value;
            if (request.ProteinG is not null) entity.ProteinG = request.ProteinG.Value;
            if (request.FatG is not null) entity.FatG = request.FatG.Value;
            if (request.CarbG is not null) entity.CarbG = request.CarbG.Value;
            if (request.SugarG is not null) entity.SugarG = request.SugarG.Value;
            if (request.FiberG is not null) entity.FiberG = request.FiberG.Value;
            if (request.SodiumMg is not null) entity.SodiumMg = request.SodiumMg.Value;
            if (request.DefaultPortionG is not null) entity.DefaultPortionG = request.DefaultPortionG.Value;

            if (request.Aliases is not null)
            {
                var existing = await _db.FoodAliases
                    .Where(a => a.FoodId == id)
                    .ToListAsync(ct);

                _db.FoodAliases.RemoveRange(existing);

                foreach (var a in request.Aliases)
                {
                    _db.FoodAliases.Add(new FoodAlias
                    {
                        FoodId = entity.Id,
                        Language = a.Language,
                        Alias = a.Alias,
                        NormalizedAlias = a.Alias.Trim().ToLowerInvariant(),
                    });
                }
            }

            await _db.SaveChangesAsync(ct);
            _cache.Remove("foods:all");

            return MapToDto(entity);
        }

        public async Task<IReadOnlyList<FoodAliasResponseDto>> GetAliasesAsync(int foodId, CancellationToken ct = default)
        {
            return await _db.FoodAliases
                .Where(a => a.FoodId == foodId)
                .Select(a => new FoodAliasResponseDto
                {
                    Id = a.Id,
                    Language = a.Language,
                    Alias = a.Alias,
                    NormalizedAlias = a.NormalizedAlias
                })
                .ToListAsync(ct);
        }

        public async Task AddAliasAsync(int foodId, FoodAliasDto request, CancellationToken ct = default)
        {
            var exists = await _db.Foods.AnyAsync(f => f.Id == foodId, ct);
            if (!exists)
                throw new KeyNotFoundException($"Food {foodId} not found.");

            _db.FoodAliases.Add(new FoodAlias
            {
                FoodId = foodId,
                Language = request.Language,
                Alias = request.Alias,
                NormalizedAlias = request.Alias.Trim().ToLowerInvariant(),
            });

            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> DeleteAliasAsync(int foodId, int aliasId, CancellationToken ct = default)
        {
            var alias = await _db.FoodAliases
                .FirstOrDefaultAsync(a => a.Id == aliasId && a.FoodId == foodId, ct);

            if (alias is null) return false;

            _db.FoodAliases.Remove(alias);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        // Mapper

        private static FoodResponseDto MapToDto(Food f) => new()
        {
            Id = f.Id,
            Name = f.Name,
            DisplayName = f.Name,
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