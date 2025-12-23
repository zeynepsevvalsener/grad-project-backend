namespace GradProject.Application.DTOs.Nutrition
{
    public class DailyTargetsDto
    {
        public decimal CalorieTarget { get; set; }

        public decimal ProteinTargetG { get; set; }
        public decimal CarbTargetG { get; set; }
        public decimal FatTargetG { get; set; }

        // Optional but helpful for frontend/debug
        public decimal BasedOnTdee { get; set; }

        public MacroSplitDto MacroSplit { get; set; } = new();
    }

    public class MacroSplitDto
    {
        // Percent values in range 0-100
        public decimal ProteinPercent { get; set; } = 30m;
        public decimal CarbPercent { get; set; } = 40m;
        public decimal FatPercent { get; set; } = 30m;
    }
}
