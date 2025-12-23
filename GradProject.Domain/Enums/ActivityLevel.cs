namespace GradProject.Domain.Enums
{
    /// <summary>
    /// Represents the user's daily activity level used to calculate TDEE.
    /// </summary>
    public enum ActivityLevel
    {
        Sedentary = 0,        // Little or no exercise
        LightlyActive = 1,    // Light exercise 1-3 days/week
        ModeratelyActive = 2, // Moderate exercise 3-5 days/week
        VeryActive = 3,       // Hard exercise 6-7 days/week
        ExtraActive = 4       // Very hard exercise / physical job / athlete
    }
}
