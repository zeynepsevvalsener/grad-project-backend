using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class ConsumedFoodService : IConsumedFoodService
    {
        private readonly AppDbContext _db;
        private readonly IDailyIntakeAggregationService _dailyAgg;

        public ConsumedFoodService(AppDbContext db, IDailyIntakeAggregationService dailyAgg)
        {
            _db = db;
            _dailyAgg = dailyAgg;
        }

        public async Task<IReadOnlyList<ConsumedFoodResponseDto>> GetByDateAsync(
            int userId, DateOnly date, CancellationToken ct = default)
        {
            var start = date.ToDateTime(TimeOnly.MinValue);
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
                    PortionG = x.PortionG,
                    MealId = x.MealId,
                    MealType = x.Meal != null ? x.Meal.MealType : (MealType?)null,
                    MealTypeName = x.Meal != null
                        ? x.Meal.MealType.ToString()
                        : null
                })
                .ToListAsync(ct);

            return items;
        }

        public async Task<ConsumedFoodResponseDto> CreateAsync(
            int userId, CreateConsumedFoodRequestDto request, CancellationToken ct = default)
        {
            var foodExists = await _db.Foods.AnyAsync(f => f.Id == request.FoodId, ct);
            if (!foodExists)
                throw new KeyNotFoundException("Food not found.");

            var consumedAt = request.ConsumedAt ?? DateTime.UtcNow;
            var date = DateOnly.FromDateTime(consumedAt);

            // O gün + o MealType için Meal var mı, yoksa oluştur
            var meal = await FindOrCreateMealAsync(userId, request.MealType, consumedAt, date, ct);

            var entity = new ConsumedFood
            {
                UserId = userId,
                FoodId = request.FoodId,
                PortionG = request.PortionG,
                ConsumedAt = consumedAt,
                MealId = meal.Id
            };

            _db.ConsumedFoods.Add(entity);

            // MealFood kaydı da oluştur (Meal içeriğini senkron tut)
            var food = await _db.Foods.AsNoTracking()
                .FirstAsync(f => f.Id == request.FoodId, ct);

            _db.MealFoods.Add(new MealFood
            {
                MealId = meal.Id,
                FoodId = request.FoodId,
                Quantity = request.PortionG,
                Unit = "g"
            });

            await _db.SaveChangesAsync(ct);
            await _dailyAgg.AggregateDailyIntakeAsync(userId, date, ct);

            return new ConsumedFoodResponseDto
            {
                Id = entity.Id,
                FoodId = entity.FoodId,
                FoodName = food.Name,
                ConsumedAt = entity.ConsumedAt,
                PortionG = entity.PortionG,
                MealId = meal.Id,
                MealType = meal.MealType,
                MealTypeName = meal.MealType.ToString()
            };
        }

        public async Task<ConsumedFoodResponseDto?> UpdateAsync(
            int userId, int consumedFoodId, UpdateConsumedFoodRequestDto request, CancellationToken ct = default)
        {
            var entity = await _db.ConsumedFoods
                .Include(x => x.Food)
                .Include(x => x.Meal)
                .FirstOrDefaultAsync(x => x.Id == consumedFoodId && x.UserId == userId, ct);

            if (entity is null)
                return null;

            var oldDate = DateOnly.FromDateTime(entity.ConsumedAt);
            var newConsumedAt = request.ConsumedAt ?? entity.ConsumedAt;
            var newDate = DateOnly.FromDateTime(newConsumedAt);

            // MealType değişti mi veya tarih değişti mi?
            var currentMealType = entity.Meal?.MealType;
            var requestedMealType = request.MealType ?? currentMealType;

            bool mealNeedsReassign = request.MealType.HasValue && request.MealType != currentMealType
                                     || newDate != oldDate;

            if (mealNeedsReassign && requestedMealType.HasValue)
            {
                // Eski MealFood kaydını sil
                var oldMealFood = await _db.MealFoods
                    .FirstOrDefaultAsync(mf => mf.MealId == entity.MealId && mf.FoodId == entity.FoodId, ct);
                if (oldMealFood != null)
                    _db.MealFoods.Remove(oldMealFood);

                // Yeni Meal bul veya oluştur
                var newMeal = await FindOrCreateMealAsync(
                    userId, requestedMealType.Value, newConsumedAt, newDate, ct);

                entity.MealId = newMeal.Id;

                // Yeni MealFood ekle
                _db.MealFoods.Add(new MealFood
                {
                    MealId = newMeal.Id,
                    FoodId = entity.FoodId,
                    Quantity = request.PortionG,
                    Unit = "g"
                });
            }
            else if (entity.MealId.HasValue)
            {
                // Aynı meal'da kaldı, sadece quantity güncelle
                var mealFood = await _db.MealFoods
                    .FirstOrDefaultAsync(mf => mf.MealId == entity.MealId && mf.FoodId == entity.FoodId, ct);
                if (mealFood != null)
                {
                    mealFood.Quantity = request.PortionG;
                }
            }

            entity.PortionG = request.PortionG;
            entity.ConsumedAt = newConsumedAt;

            await _db.SaveChangesAsync(ct);

            await _dailyAgg.AggregateDailyIntakeAsync(userId, newDate, ct);
            if (oldDate != newDate)
                await _dailyAgg.AggregateDailyIntakeAsync(userId, oldDate, ct);

            // Güncel Meal bilgisini yükle
            var updatedMeal = entity.MealId.HasValue
                ? await _db.Meals.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == entity.MealId, ct)
                : null;

            return new ConsumedFoodResponseDto
            {
                Id = entity.Id,
                FoodId = entity.FoodId,
                FoodName = entity.Food.Name,
                ConsumedAt = entity.ConsumedAt,
                PortionG = entity.PortionG,
                MealId = entity.MealId,
                MealType = updatedMeal?.MealType,
                MealTypeName = updatedMeal?.MealType.ToString()
            };
        }

        public async Task<bool> DeleteAsync(
            int userId, int consumedFoodId, CancellationToken ct = default)
        {
            var entity = await _db.ConsumedFoods
                .FirstOrDefaultAsync(x => x.Id == consumedFoodId && x.UserId == userId, ct);

            if (entity is null)
                return false;

            var date = DateOnly.FromDateTime(entity.ConsumedAt);

            // Bağlı MealFood kaydını da temizle
            if (entity.MealId.HasValue)
            {
                var mealFood = await _db.MealFoods
                    .FirstOrDefaultAsync(mf => mf.MealId == entity.MealId && mf.FoodId == entity.FoodId, ct);
                if (mealFood != null)
                    _db.MealFoods.Remove(mealFood);
            }

            _db.ConsumedFoods.Remove(entity);
            await _db.SaveChangesAsync(ct);
            await _dailyAgg.AggregateDailyIntakeAsync(userId, date, ct);

            return true;
        }

        // -------------------------------------------------------------------
        // Yardımcı: O gün + MealType için Meal bul, yoksa oluştur
        // -------------------------------------------------------------------
        private async Task<Meal> FindOrCreateMealAsync(
            int userId, MealType mealType, DateTime loggedAt, DateOnly date, CancellationToken ct)
        {
            var start = date.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);

            var existing = await _db.Meals
                .FirstOrDefaultAsync(m =>
                    m.UserId == userId &&
                    m.MealType == mealType &&
                    m.LoggedAt >= start &&
                    m.LoggedAt < end, ct);

            if (existing != null)
                return existing;

            var newMeal = new Meal
            {
                UserId = userId,
                MealType = mealType,
                LoggedAt = loggedAt,
                CreatedAt = DateTime.UtcNow
            };

            _db.Meals.Add(newMeal);
            await _db.SaveChangesAsync(ct);

            return newMeal;
        }
    }
}