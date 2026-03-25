namespace GradProject.Application.Utilities;

/// <summary>
/// Pure pace arithmetic shared across running services.
/// Pace notation used throughout: <c>minutes.seconds</c> where the decimal part represents seconds,
/// not fractional minutes (e.g. 5.45 = 5 min 45 sec per km, NOT 5.45 minutes).
/// </summary>
public static class RunningPaceCalculator
{
    /// <summary>
    /// Converts raw activity metrics to pace in min/km (minutes.seconds notation).
    /// Returns 0 when inputs are invalid.
    /// </summary>
    public static double FromDistanceAndTime(double distanceMeters, double movingTimeSeconds)
    {
        if (distanceMeters <= 0 || movingTimeSeconds <= 0)
            return 0.0;

        var secondsPerKm = (movingTimeSeconds / distanceMeters) * 1000.0;
        return FromSecondsPerKm(secondsPerKm);
    }

    /// <summary>
    /// Converts total seconds-per-km to pace in minutes.seconds notation.
    /// </summary>
    public static double FromSecondsPerKm(double totalSecondsPerKm)
    {
        if (totalSecondsPerKm <= 0)
            return 0.0;

        var minutes = Math.Floor(totalSecondsPerKm / 60.0);
        var seconds = totalSecondsPerKm % 60.0;

        // Guard against floating-point edge cases
        if (seconds >= 60.0)
        {
            minutes += Math.Floor(seconds / 60.0);
            seconds %= 60.0;
        }

        return Math.Round(minutes + (seconds / 100.0), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Converts a pace value (minutes.seconds notation) back to total seconds per km.
    /// Used when comparing or computing improvement percentages.
    /// </summary>
    public static double ToTotalSeconds(double pace)
    {
        var minutes = Math.Floor(pace);
        var seconds = (pace - minutes) * 100.0;
        return (minutes * 60.0) + seconds;
    }

    /// <summary>
    /// Calculates improvement percentage between two pace values.
    /// Positive result = improvement (faster); negative = regression (slower).
    /// </summary>
    public static double ImprovementPercent(double oldPace, double newPace)
    {
        var oldSec = ToTotalSeconds(oldPace);
        if (oldSec <= 0)
            return 0.0;

        var newSec = ToTotalSeconds(newPace);
        return ((oldSec - newSec) / oldSec) * 100.0;
    }
}
