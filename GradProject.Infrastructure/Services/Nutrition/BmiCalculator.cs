using GradProject.Application.Interfaces.Nutrition;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class BmiCalculator : IBmiCalculator
    {
        public decimal CalculateBmi(decimal weightKg, decimal heightCm)
        {
            // BMI = kg / (m^2)
            var heightM = heightCm / 100m;
            if (heightM <= 0) throw new InvalidOperationException("Height must be greater than 0.");

            var bmi = weightKg / (heightM * heightM);
            return bmi;
        }

        public string GetCategory(decimal bmi)
        {
            // WHO categories
            if (bmi < 18.5m) return "Underweight";
            if (bmi < 25m) return "Normal";
            if (bmi < 30m) return "Overweight";
            return "Obese";
        }
    }
}
