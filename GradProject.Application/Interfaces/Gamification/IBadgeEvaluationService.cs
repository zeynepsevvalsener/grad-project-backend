using GradProject.Application.DTOs.Gamification;

namespace GradProject.Application.Interfaces.Gamification
{
    /// <summary>
    /// Evaluates all badge conditions for a user and awards any newly earned badges.
    /// Safe to call repeatedly — already-owned badges are never re-awarded.
    /// Intended to be triggered after: run sync, territory claim/defend/transfer, challenge completion.
    /// </summary>
    public interface IBadgeEvaluationService
    {
        /// <summary>
        /// Checks all badge conditions for the given user and awards any badges whose
        /// conditions are now met for the first time. Idempotent — can be called multiple
        /// times without creating duplicate awards.
        /// </summary>
        /// <param name="userId">The user to evaluate.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="BadgeEvaluationResult"/> listing newly awarded badge IDs
        /// and summary counts. Returns <see cref="BadgeEvaluationResult.Empty"/> if the
        /// user does not exist.
        /// </returns>
        Task<BadgeEvaluationResult> EvaluateBadgeConditionsAsync(int userId, CancellationToken ct = default);
    }
}
