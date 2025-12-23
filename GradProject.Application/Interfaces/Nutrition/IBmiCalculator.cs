namespace GradProject.Application.Interfaces.Nutrition
{
    public interface IBmiCalculator
    {
        decimal CalculateBmi(decimal weightKg, decimal heightCm);
        string GetCategory(decimal bmi);
    }
}
