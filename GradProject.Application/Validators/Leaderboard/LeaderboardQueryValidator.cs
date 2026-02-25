using GradProject.Application.DTOs.Common;
using GradProject.Application.DTOs.Leaderboard;

namespace GradProject.Application.Validators.Leaderboard
{
    /// <summary>
    /// Validator for leaderboard query parameters
    /// </summary>
    public class LeaderboardQueryValidator
    {
        /// <summary>
        /// Validates leaderboard query parameters
        /// </summary>
        /// <param name="query">The query parameters to validate</param>
        /// <returns>ValidationResult indicating success or failure with error message</returns>
        public ValidationResult Validate(LeaderboardQuery query)
        {
            // Validate page parameter (must be >= 1)
            if (query.Page.HasValue && query.Page.Value < 1)
            {
                return ValidationResult.Error("Page must be >= 1");
            }

            // Validate pageSize parameter (must be between 1 and 100)
            if (query.PageSize.HasValue && (query.PageSize.Value < 1 || query.PageSize.Value > 100))
            {
                return ValidationResult.Error("PageSize must be between 1 and 100");
            }

            // Validate limit parameter (must be >= 1)
            if (query.Limit.HasValue && query.Limit.Value < 1)
            {
                return ValidationResult.Error("Limit must be >= 1");
            }

            return ValidationResult.Success();
        }
    }
}
