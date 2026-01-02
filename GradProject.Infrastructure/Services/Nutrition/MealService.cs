using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class MealService : IMealService
    {
        private readonly AppDbContext _db;

        // Unit conversion dictionaries (reused from MealParsingService)
        private static readonly Dictionary<string, decimal> UnitToGramMultiplier = new(StringComparer.OrdinalIgnoreCase)
        {
            ["g"] = 1m,
            ["gr"] = 1m,
            ["gram"] = 1m,
            ["grams"] = 1m,
            ["kg"] = 1000m,
            ["kilogram"] = 1000m,
            ["kilograms"] = 1000m,
            ["ml"] = 1m,
            ["milliliter"] = 1m,
            ["milliliters"] = 1m,
            ["l"] = 1000m,
            ["lt"] = 1000m,
            ["liter"] = 1000m,
            ["liters"] = 1000m,
            ["cup"] = 200m,
            ["cups"] = 200m,
            ["tbsp"] = 15m,
            ["tablespoon"] = 15m,
            ["tablespoons"] = 15m,
            ["tsp"] = 5m,
            ["teaspoon"] = 5m,
            ["teaspoons"] = 5m,
        };

        private static readonly HashSet<string> CountUnits = new(StringComparer.OrdinalIgnoreCase)
        {
            "piece", "pieces", "pc", "pcs",
            "slice", "slices",
            "adet", "tane"
        };

        public MealService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<MealResponseDto>> GetByDateAsync(int userId, DateOnly date, CancellationToken ct = default)
        {
            var start = date.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);

            var meals = await _db.Meals
                .AsNoTracking()
                .Where(m => m.UserId == userId && m.LoggedAt >= start && m.LoggedAt < end)
                .Include(m => m.MealFoods)
                    .ThenInclude(mf => mf.Food)
                .OrderByDescending(m => m.LoggedAt)
                .ToListAsync(ct);

            return meals.Select(m => MapToResponseDto(m)).ToList();
        }

        public async Task<MealResponseDto?> GetByIdAsync(int userId, int mealId, CancellationToken ct = default)
        {
            var meal = await _db.Meals
                .AsNoTracking()
                .Where(m => m.Id == mealId && m.UserId == userId)
                .Include(m => m.MealFoods)
                    .ThenInclude(mf => mf.Food)
                .FirstOrDefaultAsync(ct);

            return meal == null ? null : MapToResponseDto(meal);
        }

        public async Task<MealResponseDto> CreateAsync(int userId, CreateMealRequestDto request, CancellationToken ct = default)
        {
            // Validate foods exist if provided
            if (request.Foods != null && request.Foods.Any())
            {
                var foodIds = request.Foods.Select(f => f.FoodId).Distinct().ToList();
                var existingFoods = await _db.Foods
                    .Where(f => foodIds.Contains(f.Id))
                    .Select(f => f.Id)
                    .ToListAsync(ct);

                var missingFoods = foodIds.Except(existingFoods).ToList();
                if (missingFoods.Any())
                {
                    throw new KeyNotFoundException($"Food(s) not found: {string.Join(", ", missingFoods)}");
                }
            }

            var meal = new Meal
            {
                UserId = userId,
                MealType = request.MealType,
                LoggedAt = request.LoggedAt,
                RawText = request.RawText,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _db.Meals.Add(meal);
            await _db.SaveChangesAsync(ct);

            // Add MealFood records if provided
            if (request.Foods != null && request.Foods.Any())
            {
                var foods = await _db.Foods
                    .Where(f => request.Foods.Select(mf => mf.FoodId).Contains(f.Id))
                    .ToListAsync(ct);

                var mealFoods = new List<MealFood>();
                foreach (var foodDto in request.Foods)
                {
                    var food = foods.First(f => f.Id == foodDto.FoodId);
                    var portionG = ConvertToGrams(foodDto.Quantity, foodDto.Unit, food.DefaultPortionG);

                    mealFoods.Add(new MealFood
                    {
                        MealId = meal.Id,
                        FoodId = foodDto.FoodId,
                        Quantity = foodDto.Quantity,
                        Unit = foodDto.Unit
                    });

                    // Create ConsumedFood record for nutrition tracking (Option B from requirements)
                    var consumedFood = new ConsumedFood
                    {
                        UserId = userId,
                        FoodId = foodDto.FoodId,
                        PortionG = portionG,
                        ConsumedAt = request.LoggedAt
                    };
                    _db.ConsumedFoods.Add(consumedFood);
                }

                _db.MealFoods.AddRange(mealFoods);
                await _db.SaveChangesAsync(ct);
            }

            // Reload with includes for response
            var createdMeal = await _db.Meals
                .Include(m => m.MealFoods)
                    .ThenInclude(mf => mf.Food)
                .FirstAsync(m => m.Id == meal.Id, ct);

            return MapToResponseDto(createdMeal);
        }

        public async Task<MealResponseDto?> UpdateAsync(int userId, int mealId, UpdateMealRequestDto request, CancellationToken ct = default)
        {
            var meal = await _db.Meals
                .Include(m => m.MealFoods)
                .FirstOrDefaultAsync(m => m.Id == mealId && m.UserId == userId, ct);

            if (meal == null)
                return null;

            // Validate foods exist if provided
            if (request.Foods != null && request.Foods.Any())
            {
                var foodIds = request.Foods.Select(f => f.FoodId).Distinct().ToList();
                var existingFoods = await _db.Foods
                    .Where(f => foodIds.Contains(f.Id))
                    .Select(f => f.Id)
                    .ToListAsync(ct);

                var missingFoods = foodIds.Except(existingFoods).ToList();
                if (missingFoods.Any())
                {
                    throw new KeyNotFoundException($"Food(s) not found: {string.Join(", ", missingFoods)}");
                }
            }

            // Update meal properties
            meal.MealType = request.MealType;
            meal.LoggedAt = request.LoggedAt;
            meal.RawText = request.RawText;
            meal.Notes = request.Notes;
            meal.UpdatedAt = DateTime.UtcNow;

            // Delete old MealFoods (cascade will handle cleanup)
            _db.MealFoods.RemoveRange(meal.MealFoods);

            // Add new MealFoods if provided
            if (request.Foods != null && request.Foods.Any())
            {
                var foods = await _db.Foods
                    .Where(f => request.Foods.Select(mf => mf.FoodId).Contains(f.Id))
                    .ToListAsync(ct);

                var mealFoods = new List<MealFood>();
                foreach (var foodDto in request.Foods)
                {
                    var food = foods.First(f => f.Id == foodDto.FoodId);
                    var portionG = ConvertToGrams(foodDto.Quantity, foodDto.Unit, food.DefaultPortionG);

                    mealFoods.Add(new MealFood
                    {
                        MealId = meal.Id,
                        FoodId = foodDto.FoodId,
                        Quantity = foodDto.Quantity,
                        Unit = foodDto.Unit
                    });

                    // Create ConsumedFood record for nutrition tracking
                    var consumedFood = new ConsumedFood
                    {
                        UserId = userId,
                        FoodId = foodDto.FoodId,
                        PortionG = portionG,
                        ConsumedAt = request.LoggedAt
                    };
                    _db.ConsumedFoods.Add(consumedFood);
                }

                _db.MealFoods.AddRange(mealFoods);
            }

            await _db.SaveChangesAsync(ct);

            // Reload with includes for response
            var updatedMeal = await _db.Meals
                .Include(m => m.MealFoods)
                    .ThenInclude(mf => mf.Food)
                .FirstAsync(m => m.Id == meal.Id, ct);

            return MapToResponseDto(updatedMeal);
        }

        public async Task<bool> DeleteAsync(int userId, int mealId, CancellationToken ct = default)
        {
            var meal = await _db.Meals
                .Include(m => m.MealFoods)
                .FirstOrDefaultAsync(m => m.Id == mealId && m.UserId == userId, ct);

            if (meal == null)
                return false;

            // Delete related ConsumedFood records (optional - for cleanup)
            // Note: This is a simple approach. In production, you might want to track
            // which ConsumedFoods were created by meals vs manually added.
            // For now, we'll just delete the meal and let MealFoods cascade delete.

            _db.Meals.Remove(meal);
            await _db.SaveChangesAsync(ct);

            return true;
        }

        private static decimal ConvertToGrams(decimal quantity, string unit, decimal defaultPortionG)
        {
            if (!string.IsNullOrWhiteSpace(unit))
            {
                if (UnitToGramMultiplier.TryGetValue(unit, out var mult))
                    return Math.Round(quantity * mult, 2);

                if (CountUnits.Contains(unit))
                {
                    var baseG = defaultPortionG > 0 ? defaultPortionG : 100m;
                    return Math.Round(quantity * baseG, 2);
                }
            }

            if (defaultPortionG > 0)
                return Math.Round(quantity * defaultPortionG, 2);

            return Math.Round(quantity * 100m, 2);
        }

        private static MealResponseDto MapToResponseDto(Meal meal)
        {
            var mealTypeName = meal.MealType switch
            {
                Domain.Enums.MealType.BREAKFAST => "Breakfast",
                Domain.Enums.MealType.LUNCH => "Lunch",
                Domain.Enums.MealType.DINNER => "Dinner",
                Domain.Enums.MealType.SNACK => "Snack",
                _ => meal.MealType.ToString()
            };

            var foods = meal.MealFoods.Select(mf =>
            {
                var portionG = ConvertToGrams(mf.Quantity, mf.Unit, mf.Food.DefaultPortionG);
                return new MealFoodResponseDto
                {
                    Id = mf.Id,
                    FoodId = mf.FoodId,
                    FoodName = mf.Food.Name,
                    Quantity = mf.Quantity,
                    Unit = mf.Unit,
                    PortionG = portionG
                };
            }).ToList();

            return new MealResponseDto
            {
                Id = meal.Id,
                MealType = meal.MealType,
                MealTypeName = mealTypeName,
                LoggedAt = meal.LoggedAt,
                RawText = meal.RawText,
                Notes = meal.Notes,
                Foods = foods,
                CreatedAt = meal.CreatedAt,
                UpdatedAt = meal.UpdatedAt
            };
        }
    }
}

