namespace GradProject.Application.Options;

public sealed class TerritoryCatalogCacheOptions
{
    public const string SectionName = "TerritoryCatalog";

    /// <summary>When true, GET /api/v1/territory list is cached in memory.</summary>
    public bool EnableListCache { get; set; } = true;

    /// <summary>Absolute expiration for the catalog cache entry.</summary>
    public int ListCacheSeconds { get; set; } = 120;
}
