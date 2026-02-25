namespace GradProject.Application.DTOs.Common
{
    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Indicates whether the validation was successful
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        public static ValidationResult Success() => new ValidationResult { IsValid = true };

        /// <summary>
        /// Creates a failed validation result with an error message
        /// </summary>
        public static ValidationResult Error(string message) => new ValidationResult
        {
            IsValid = false,
            ErrorMessage = message
        };
    }
}
