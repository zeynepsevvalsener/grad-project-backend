using GradProject.Domain.Enums;

namespace GradProject.Application.Interfaces.Nutrition
{
    public interface ITdeeCalculator
    {
        decimal GetActivityFactor(ActivityLevel level);

        decimal CalculateBmr(decimal weightKg, decimal heightCm, int ageYears, Gender gender);

        decimal CalculateTdee(decimal bmr, ActivityLevel level);
    }
}
