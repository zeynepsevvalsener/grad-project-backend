using GradProject.Application.DTOs.Common;
using GradProject.Application.DTOs.Nutrition;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class FoodSearchService : IFoodSearchService
    {
        private readonly AppDbContext _db;

        public FoodSearchService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResultDto<FoodSearchItemDto>> SearchAsync(
            string? query,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            // normalize paging
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var q = (query ?? string.Empty).Trim();

            IQueryable<Domain.Entities.Food> baseQuery = _db.Foods.AsNoTracking();

            // If empty query -> return paged list (by name)
            if (string.IsNullOrWhiteSpace(q))
            {
                return await ToPagedResultAsync(
                    baseQuery.OrderBy(f => f.Name),
                    page,
                    pageSize,
                    ct);
            }

            var qLower = q.ToLowerInvariant();

            // 1) Category detection: if q matches any category exactly (case-insensitive)
            var isCategoryQuery = await baseQuery
                .AnyAsync(f => f.Category.ToLower() == qLower, ct);

            IQueryable<Domain.Entities.Food> filtered;

            if (isCategoryQuery)
            {
                // list all foods in that category
                filtered = baseQuery
                    .Where(f => f.Category.ToLower() == qLower)
                    .OrderBy(f => f.Name);
            }
            else
            {
                // 2) text search in name + aliases (case-insensitive contains)
                var pattern = $"%{q}%";

                filtered = baseQuery
                    .Where(f =>
                        EF.Functions.ILike(f.Name, pattern) ||
                        (f.Aliases != null && f.Aliases.Any(a => EF.Functions.ILike(a, pattern))))
                    .OrderBy(f => f.Name);
            }

            return await ToPagedResultAsync(filtered, page, pageSize, ct);
        }

        private async Task<PagedResultDto<FoodSearchItemDto>> ToPagedResultAsync(
            IQueryable<Domain.Entities.Food> query,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            var totalCount = await query.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // if page beyond range, return empty page (safe for frontend)
            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var skip = (page - 1) * pageSize;

            var items = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(f => new FoodSearchItemDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Category = f.Category,

                    Kcal = f.Kcal,
                    ProteinG = f.ProteinG,
                    FatG = f.FatG,
                    CarbG = f.CarbG,

                    SugarG = f.SugarG,
                    FiberG = f.FiberG,
                    SodiumMg = f.SodiumMg,

                    DefaultPortionG = f.DefaultPortionG,

                    Source = f.Source,
                    Aliases = f.Aliases ?? Array.Empty<string>()
                })
                .ToListAsync(ct);

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
    }
}
