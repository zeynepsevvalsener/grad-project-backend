using GradProject.Application.DTOs.Nutrition;

namespace GradProject.Application.Interfaces.Nutrition
{
    public interface IDailyIntakeAggregationService
    {
        /// <summary>
        /// Aggregates all food logs (ConsumedFood) for a given user and date,
        /// calculates total calories, protein, carbs, and fat,
        /// and updates or creates the DailySummary record.
        /// </summary>
        /// <param name="userId">The user ID to aggregate intake for</param>
        /// <param name="date">The date to aggregate intake for</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task AggregateDailyIntakeAsync(int userId, DateOnly date, CancellationToken ct = default);

        /// <summary>
        /// Gets the DailySummary for a given user and date
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="date">The date</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>DailySummaryDto if found, null otherwise</returns>
        Task<DailySummaryDto?> GetDailySummaryAsync(int userId, DateOnly date, CancellationToken ct = default);
    }
}

