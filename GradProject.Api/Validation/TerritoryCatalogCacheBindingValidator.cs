using GradProject.Application.Options;
using Microsoft.Extensions.Options;

namespace GradProject.Api.Validation;

public sealed class TerritoryCatalogCacheBindingValidator : IValidateOptions<TerritoryCatalogCacheOptions>
{
    public ValidateOptionsResult Validate(string? name, TerritoryCatalogCacheOptions options)
    {
        if (options.EnableListCache && options.ListCacheSeconds is < 5 or > 86_400)
        {
            return ValidateOptionsResult.Fail(
                "TerritoryCatalog:ListCacheSeconds must be between 5 and 86400 when caching is enabled.");
        }

        return ValidateOptionsResult.Success;
    }
}
