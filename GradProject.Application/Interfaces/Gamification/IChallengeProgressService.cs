namespace GradProject.Application.Interfaces.Gamification
{
    public interface IChallengeProgressService
    {
        /// <summary>
        /// Updates challenge progress after a run is saved.
        /// This method should be called after RunningActivity is successfully saved to the database.
        /// </summary>
        /// <param name="runId">The ID of the RunningActivity that was just saved</param>
        /// <param name="ct">Cancellation token</param>
        Task UpdateAfterRunSaved(int runId, CancellationToken ct = default);

        /// <summary>
        /// Updates challenge progress after nutrition data is saved (meal or consumed food).
        /// This method should be called after DailySummary is updated or when calories are consumed.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="totalCalories">The total calories consumed for the day (will be used to update progress incrementally)</param>
        /// <param name="date">The date for which calories were consumed</param>
        /// <param name="ct">Cancellation token</param>
        Task UpdateAfterNutritionSaved(int userId, int totalCalories, DateOnly date, CancellationToken ct = default);
    }
}

