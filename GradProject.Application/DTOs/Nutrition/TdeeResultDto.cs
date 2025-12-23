namespace GradProject.Application.DTOs.Nutrition
{
    public class TdeeResultDto
    {
        public int Age { get; set; }
        public decimal HeightCm { get; set; }
        public decimal WeightKg { get; set; }

        public string Gender { get; set; } = null!;
        public string ActivityLevel { get; set; } = null!;
        public decimal ActivityFactor { get; set; }

        public decimal Bmr { get; set; }
        public decimal Tdee { get; set; }

        public decimal Bmi { get; set; }
        public string BmiCategory { get; set; } = null!;

    }
}
