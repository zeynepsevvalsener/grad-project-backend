using GradProject.Application.Interfaces.Nutrition;
using GradProject.Domain.Enums;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class TdeeCalculator : ITdeeCalculator
    {
        public decimal GetActivityFactor(ActivityLevel level) => level switch
        {
            ActivityLevel.Sedentary => 1.2m,
            ActivityLevel.LightlyActive => 1.375m,
            ActivityLevel.ModeratelyActive => 1.55m,
            ActivityLevel.VeryActive => 1.725m,
            ActivityLevel.ExtraActive => 1.9m,
            _ => 1.2m
        };

        public decimal CalculateBmr(decimal weightKg, decimal heightCm, int ageYears, Gender gender)
        {
            // Mifflin–St Jeor:
            // male:    10W + 6.25H - 5A + 5
            // female:  10W + 6.25H - 5A - 161
            var baseValue = (10m * weightKg) + (6.25m * heightCm) - (5m * ageYears);

            return gender switch
            {
                Gender.Male => baseValue + 5m,
                Gender.Female => baseValue - 161m,
                _ => throw new InvalidOperationException("To calculate BMR/TDEE, please set your gender.")
            };
        }

        public decimal CalculateTdee(decimal bmr, ActivityLevel level)
        {
            var factor = GetActivityFactor(level);
            return bmr * factor;
        }
    }
}
