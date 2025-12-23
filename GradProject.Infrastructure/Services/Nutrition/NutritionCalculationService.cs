using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class NutritionCalculationService : INutritionCalculationService
    {
        private readonly AppDbContext _db;
        private readonly ITdeeCalculator _calculator;
        private readonly IBmiCalculator _bmiCalculator;
        public NutritionCalculationService(AppDbContext db, ITdeeCalculator calculator, IBmiCalculator bmiCalculator)
        {
            _db = db;
            _calculator = calculator;
            _bmiCalculator = bmiCalculator;
        }

        public async Task<TdeeResultDto> GetMyTdeeAsync(int userId, CancellationToken ct = default)
        {
            var profile = await _db.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile is null)
                throw new InvalidOperationException("Profile not found. Please create your profile first.");

            if (profile.DateOfBirth is null)
                throw new InvalidOperationException("To calculate TDEE, please set your date of birth.");

            if (profile.Height is null || profile.Height <= 0)
                throw new InvalidOperationException("To calculate TDEE, please set your height.");

            if (profile.Weight is null || profile.Weight <= 0)
                throw new InvalidOperationException("To calculate TDEE, please set your weight.");

            // Age calc
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dob = DateOnly.FromDateTime(profile.DateOfBirth.Value);

            var age = today.Year - dob.Year;
            if (dob > today.AddYears(-age)) age--;

            if (age < 10 || age > 120)
                throw new InvalidOperationException("Date of birth is not valid for TDEE calculation.");

            var weightKg = profile.Weight.Value;
            var heightCm = profile.Height.Value;

            var bmi = _bmiCalculator.CalculateBmi(weightKg, heightCm);
            var bmiRounded = decimal.Round(bmi, 1);
            var bmiCategory = _bmiCalculator.GetCategory(bmiRounded);

            var bmr = _calculator.CalculateBmr(weightKg, heightCm, age, profile.Gender);
            var factor = _calculator.GetActivityFactor(profile.ActivityLevel);
            var tdee = _calculator.CalculateTdee(bmr, profile.ActivityLevel);

            return new TdeeResultDto
            {
                Age = age,
                HeightCm = heightCm,
                WeightKg = weightKg,
                Gender = profile.Gender.ToString(),
                ActivityLevel = profile.ActivityLevel.ToString(),
                ActivityFactor = factor,
                Bmr = decimal.Round(bmr, 0),
                Tdee = decimal.Round(tdee, 0),
                Bmi = bmiRounded,
                BmiCategory = bmiCategory
            };
        }
    }
}
