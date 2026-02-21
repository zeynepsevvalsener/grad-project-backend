namespace GradProject.Application.Interfaces.Nutrition
{
    public interface IFoodCanonicalResolver
    {
        Task<(int? foodId, decimal confidence, string? matchedAlias)> ResolveAsync(
            string input,
            string language,
            CancellationToken ct = default);
    }
}