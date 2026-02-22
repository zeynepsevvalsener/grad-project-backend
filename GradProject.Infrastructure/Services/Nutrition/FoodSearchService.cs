using GradProject.Application.DTOs.Common;
using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class FoodSearchService : IFoodSearchService
    {
        private readonly AppDbContext _db;
        private readonly ICurrentLanguage _currentLanguage;

        public FoodSearchService(AppDbContext db, ICurrentLanguage currentLanguage)
        {
            _db = db;
            _currentLanguage = currentLanguage;
        }

        public async Task<PagedResultDto<FoodSearchItemDto>> SearchAsync(
            string? query,
            string? category,
            string? sortBy,
            string? sortDir,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var q = (query ?? string.Empty).Trim();
            var cat = (category ?? string.Empty).Trim();
            var lang = NormalizeLang(_currentLanguage.Value);

            IQueryable<Domain.Entities.Food> foods = _db.Foods.AsNoTracking();

            // Category filter (case-insensitive)
            if (!string.IsNullOrWhiteSpace(cat))
            {
                // Npgsql ILIKE works well and keeps it simple
                foods = foods.Where(f => EF.Functions.ILike(f.Category, cat));
            }

            // Text query filter
            if (!string.IsNullOrWhiteSpace(q))
            {
                var pattern = $"%{q}%";

                // Language-aware alias match (current language)
                foods = foods.Where(f =>
                    EF.Functions.ILike(f.Name, pattern) ||
                    _db.FoodAliases.Any(a =>
                        a.FoodId == f.Id &&
                        a.Language == lang &&
                        (EF.Functions.ILike(a.Alias, pattern) || EF.Functions.ILike(a.NormalizedAlias, pattern))
                    )
                );
            }

            // Sorting
            foods = ApplySorting(foods, sortBy, sortDir);

            // Pagination + map
            return await ToPagedResultAsync(foods, lang, page, pageSize, ct);
        }

        private async Task<PagedResultDto<FoodSearchItemDto>> ToPagedResultAsync(
            IQueryable<Domain.Entities.Food> query,
            string lang,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            var totalCount = await query.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var skip = (page - 1) * pageSize;

            var foods = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(ct);

            var foodIds = foods.Select(f => f.Id).ToList();

            // Localized DisplayName map: only current language aliases
            // Choose one alias per food (first alphabetically)
            var displayNameMap = await _db.FoodAliases
                .AsNoTracking()
                .Where(a => foodIds.Contains(a.FoodId) && a.Language == lang)
                .OrderBy(a => a.Alias)
                .GroupBy(a => a.FoodId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(x => x.Alias).FirstOrDefault(),
                    ct);

            var items = foods.Select(f =>
            {
                displayNameMap.TryGetValue(f.Id, out var localized);
                var displayName = string.IsNullOrWhiteSpace(localized) ? f.Name : localized;

                return new FoodSearchItemDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    DisplayName = displayName,
                    Category = f.Category,

                    Kcal = f.Kcal,
                    ProteinG = f.ProteinG,
                    FatG = f.FatG,
                    CarbG = f.CarbG,

                    SugarG = f.SugarG,
                    FiberG = f.FiberG,
                    SodiumMg = f.SodiumMg,

                    DefaultPortionG = f.DefaultPortionG,
                    Source = f.Source
                };
            }).ToList();

            return new PagedResultDto<FoodSearchItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        private static IQueryable<Domain.Entities.Food> ApplySorting(
            IQueryable<Domain.Entities.Food> query,
            string? sortBy,
            string? sortDir)
        {
            var by = (sortBy ?? "alphabetical").Trim().ToLowerInvariant();
            var dir = (sortDir ?? "asc").Trim().ToLowerInvariant();
            var desc = dir == "desc";

            return by switch
            {
                "calories" or "kcal" => desc
                    ? query.OrderByDescending(f => f.Kcal).ThenBy(f => f.Name)
                    : query.OrderBy(f => f.Kcal).ThenBy(f => f.Name),

                "protein" => desc
                    ? query.OrderByDescending(f => f.ProteinG).ThenBy(f => f.Name)
                    : query.OrderBy(f => f.ProteinG).ThenBy(f => f.Name),

                "alphabetical" or "name" => desc
                    ? query.OrderByDescending(f => f.Name)
                    : query.OrderBy(f => f.Name),

                _ => query.OrderBy(f => f.Name)
            };
        }

        private static string NormalizeLang(string? lang)
        {
            // Middleware/endpoint "tr-TR" gibi set ediyorsa bile biz "tr" / "en" map edelim
            if (string.IsNullOrWhiteSpace(lang)) return "en";
            lang = lang.Trim().ToLowerInvariant();
            if (lang.Length >= 2) return lang.Substring(0, 2);
            return "en";
        }
    }
}