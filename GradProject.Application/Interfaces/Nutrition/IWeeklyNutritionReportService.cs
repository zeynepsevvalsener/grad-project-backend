using GradProject.Application.DTOs.Nutrition;

namespace GradProject.Application.Interfaces.Nutrition
{
    public interface IWeeklyNutritionReportService
    {
        Task<WeeklyNutritionReportDto> GetWeeklyReportAsync(
            int userId,
            DateOnly weekStart,
            CancellationToken ct = default);
    }
}