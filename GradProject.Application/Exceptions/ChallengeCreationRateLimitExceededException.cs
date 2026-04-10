namespace GradProject.Application.Exceptions
{
    /// <summary>Thrown when a user exceeds configured custom challenge creation limits.</summary>
    public sealed class ChallengeCreationRateLimitExceededException : Exception
    {
        public ChallengeCreationRateLimitExceededException(string message)
            : base(message)
        {
        }
    }
}
