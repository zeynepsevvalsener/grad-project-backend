using GradProject.Application.DTOs.Gamification;

namespace GradProject.Application.Interfaces.Gamification;

/// <summary>
/// Territory catalog reads and per-user progress (query side).
/// Call <see cref="InvalidateCatalogCache"/> after any ownership change so the cached catalog stays fresh.
/// </summary>
public interface ITerritoryCatalogService
{
    Task<IReadOnlyList<TerritoryCatalogItemDto>> GetCatalogAsync(CancellationToken ct = default);

    Task<IReadOnlyList<MyTerritoryProgressRowDto>> GetMyProgressAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Resolves public <see cref="Guid"/> territory IDs (as returned by the catalog) to their internal
    /// integer primary keys. Any Guid not found in the database is silently omitted from the result.
    /// </summary>
    Task<IReadOnlyList<int>> ResolvePublicIdsAsync(IReadOnlyList<Guid> publicIds, CancellationToken ct = default);

    /// <summary>
    /// Removes the in-memory catalog cache entry so the next call re-queries the database.
    /// Should be called by claim/defend operations after ownership changes.
    /// </summary>
    void InvalidateCatalogCache();
}
