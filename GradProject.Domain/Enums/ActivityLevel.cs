namespace GradProject.Domain.Enums
{
    /// <summary>
    /// Represents the user's daily activity level used to calculate TDEE.
    /// Multipliers will be defined in the application/service layer.
    /// </summary>
    public enum ActivityLevel
    {
        Sedentary = 0,   // Little or no exercise
        Light = 1,       // Light exercise 1-3 days/week
        Moderate = 2,    // Moderate exercise 3-5 days/week
        VeryActive = 3,  // Hard exercise 6-7 days/week
        Athlete = 4      // Very hard exercise / physical job / athlete
    }
}
