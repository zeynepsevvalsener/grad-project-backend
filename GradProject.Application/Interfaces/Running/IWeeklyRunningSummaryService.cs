using GradProject.Application.DTOs.Running;

namespace GradProject.Application.Interfaces.Running
{
    public interface IWeeklyRunningSummaryService
    {
        /// <summary>
        /// Aggregates running activities for an ISO week (Monday–Sunday).
        /// When isoYear and isoWeek are null, uses the current ISO week (UTC calendar date).
        /// </summary>
        Task<WeeklyRunningSummaryDto> GetWeeklySummaryAsync(
            int userId,
            int? isoYear,
            int? isoWeek,
            bool includeWeekOverWeek = true,
            CancellationToken ct = default);
    }
}
