using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Application.Services.Gamification;
using GradProject.Application.Utilities;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using GradProject.Application.Options;
using Microsoft.Extensions.Options;

namespace GradProject.Infrastructure.Services.Gamification;

public sealed class TerritoryCatalogService : ITerritoryCatalogService
{
    private const string CatalogCacheKey = "territory:catalog:v1";

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly TerritoryCatalogCacheOptions _cacheOptions;

    public TerritoryCatalogService(
        AppDbContext db,
        IMemoryCache cache,
        IOptions<TerritoryCatalogCacheOptions> cacheOptions)
    {
        _db = db;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
    }

    public async Task<IReadOnlyList<TerritoryCatalogItemDto>> GetCatalogAsync(CancellationToken ct = default)
    {
        if (_cacheOptions.EnableListCache &&
            _cache.TryGetValue(CatalogCacheKey, out IReadOnlyList<TerritoryCatalogItemDto>? cached) &&
            cached is not null)
        {
            return cached;
        }

        var rows = await _db.Territories
            .AsNoTracking()
            .Include(t => t.CurrentOwnerUser)
                .ThenInclude(u => u!.Profile)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Id)
            .ToListAsync(ct);

        var territoryIds = rows.Select(t => t.Id).ToList();
        var cellLookup = await LoadCellLookupAsync(territoryIds, ct);

        var list = new List<TerritoryCatalogItemDto>(rows.Count);
        foreach (var t in rows)
        {
            if (!cellLookup.TryGetValue(t.Id, out var cells))
                cells = new List<string>();
            var geometry = TerritoryGeometryResolver.Resolve(cells, t.GeometryCells);

            list.Add(new TerritoryCatalogItemDto
            {
                Id = t.PublicId,
                Name = t.Name,
                Description = t.Description,
                GeometryCells = geometry,
                CurrentOwnerUserId = t.CurrentOwnerUserId,
                CurrentOwnerUsername = UserDisplayNameHelper.ResolveNullable(
                    t.CurrentOwnerUser?.Profile?.FirstName,
                    t.CurrentOwnerUser?.Profile?.LastName,
                    t.CurrentOwnerUser?.Email),
                CurrentOwnerScoreSnapshot = t.CurrentOwnerScoreSnapshot,
                OwnershipTargetPercent = t.OwnershipTargetPercent,
                IsActive = t.IsActive
            });
        }

        if (_cacheOptions.EnableListCache)
        {
            _cache.Set(
                CatalogCacheKey,
                (IReadOnlyList<TerritoryCatalogItemDto>)list,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_cacheOptions.ListCacheSeconds)
                });
        }

        return list;
    }

    public async Task<IReadOnlyList<MyTerritoryProgressRowDto>> GetMyProgressAsync(int userId, CancellationToken ct = default)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));

        var territories = await _db.Territories
            .AsNoTracking()
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Id)
            .Select(t => new
            {
                t.Id,
                t.PublicId,
                t.Name,
                t.OwnershipTargetPercent,
                t.CurrentOwnerUserId
            })
            .ToListAsync(ct);

        var progressByTerritory = await _db.UserTerritories
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.TerritoryId, ct);

        var result = new List<MyTerritoryProgressRowDto>(territories.Count);
        foreach (var t in territories)
        {
            progressByTerritory.TryGetValue(t.Id, out var row);
            var isCurrentOwner = t.CurrentOwnerUserId == userId;
            var status = TerritoryProgressRules.ResolveDisplayStatus(row, t.OwnershipTargetPercent, isCurrentOwner);
            var progressPercent = status == TerritoryStatus.Locked ? 0 : row?.ProgressPercent ?? 0;

            result.Add(new MyTerritoryProgressRowDto
            {
                TerritoryId = t.PublicId,
                Name = t.Name,
                Status = status,
                ProgressPercent = progressPercent,
                OwnedAt = row?.OwnedAt,
                IsCurrentOwner = t.CurrentOwnerUserId == userId
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<int>> ResolvePublicIdsAsync(
        IReadOnlyList<Guid> publicIds,
        CancellationToken ct = default)
    {
        if (publicIds.Count == 0)
            return Array.Empty<int>();

        return await _db.Territories
            .AsNoTracking()
            .Where(t => publicIds.Contains(t.PublicId))
            .Select(t => t.Id)
            .ToListAsync(ct);
    }

    public void InvalidateCatalogCache() => _cache.Remove(CatalogCacheKey);

    private async Task<Dictionary<int, List<string>>> LoadCellLookupAsync(
        IReadOnlyList<int> territoryIds,
        CancellationToken ct)
    {
        if (territoryIds.Count == 0)
            return new Dictionary<int, List<string>>();

        var cellRows = await _db.TerritoryCells
            .AsNoTracking()
            .Where(c => territoryIds.Contains(c.TerritoryId))
            .Select(c => new { c.TerritoryId, c.H3Index })
            .ToListAsync(ct);

        return cellRows
            .GroupBy(x => x.TerritoryId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.H3Index).Distinct().ToList());
    }

}
