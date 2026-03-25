namespace GradProject.Application.Utilities;

/// <summary>
/// Centralises the "full name or email fallback" display name rule used throughout the codebase.
/// </summary>
public static class UserDisplayNameHelper
{
    /// <summary>
    /// Returns <c>FirstName LastName</c> (trimmed) when <paramref name="firstName"/> is present;
    /// otherwise falls back to <paramref name="fallback"/> (typically the email address).
    /// </summary>
    public static string Resolve(string? firstName, string? lastName, string fallback) =>
        !string.IsNullOrWhiteSpace(firstName)
            ? $"{firstName} {lastName}".Trim()
            : fallback;

    /// <summary>
    /// Nullable variant — same rule but returns <c>null</c> when both name and fallback are absent.
    /// </summary>
    public static string? ResolveNullable(string? firstName, string? lastName, string? fallback) =>
        !string.IsNullOrWhiteSpace(firstName)
            ? $"{firstName} {lastName}".Trim()
            : fallback;
}
