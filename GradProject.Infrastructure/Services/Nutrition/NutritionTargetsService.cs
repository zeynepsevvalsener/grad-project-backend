using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Nutrition;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class NutritionTargetsService : INutritionTargetsService
    {
        private readonly INutritionCalculationService _nutritionCalculationService;

        public NutritionTargetsService(INutritionCalculationService nutritionCalculationService)
        {
            _nutritionCalculationService = nutritionCalculationService;
        }

        public async Task<DailyTargetsDto> GetMyDailyTargetsAsync(int userId, CancellationToken ct = default)
        {
            // Reuse existing service (BMR/TDEE/BMI)
            var tdeeResult = await _nutritionCalculationService.GetMyTdeeAsync(userId, ct);

            var calorieTarget = tdeeResult.Tdee; // maintenance for MVP

            // Default macro split for MVP
            var split = new MacroSplitDto
            {
                ProteinPercent = 30m,
                CarbPercent = 40m,
                FatPercent = 30m
            };

            // kcal distribution
            var proteinKcal = calorieTarget * (split.ProteinPercent / 100m);
            var carbKcal = calorieTarget * (split.CarbPercent / 100m);
            var fatKcal = calorieTarget * (split.FatPercent / 100m);

            // grams conversion (Protein/Carb: 4 kcal per g, Fat: 9 kcal per g)
            var proteinG = proteinKcal / 4m;
            var carbG = carbKcal / 4m;
            var fatG = fatKcal / 9m;

            return new DailyTargetsDto
            {
                CalorieTarget = decimal.Round(calorieTarget, 0),
                ProteinTargetG = decimal.Round(proteinG, 1),
                CarbTargetG = decimal.Round(carbG, 1),
                FatTargetG = decimal.Round(fatG, 1),

                BasedOnTdee = decimal.Round(calorieTarget, 0),
                MacroSplit = split
            };
        }
    }
}
