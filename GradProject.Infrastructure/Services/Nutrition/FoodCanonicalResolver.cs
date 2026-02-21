using GradProject.Application.Interfaces.Nutrition;
using GradProject.Application.Utilities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Nutrition
{
    public class FoodCanonicalResolver : IFoodCanonicalResolver
    {
        private readonly AppDbContext _db;

        public FoodCanonicalResolver(AppDbContext db)
        {
            _db = db;
        }

        public async Task<(int? foodId, decimal confidence, string? matchedAlias)> ResolveAsync(
            string input,
            string language,
            CancellationToken ct = default)
        {
            var norm = FoodTextNormalizer.Normalize(input);
            if (string.IsNullOrWhiteSpace(norm))
                return (null, 0m, null);

            var lang = NormalizeLang(language);

            // 1) Exact alias (lang then en)
            var exact = await FindExactAliasAsync(norm, lang, ct)
                        ?? await FindExactAliasAsync(norm, "en", ct);

            if (exact.HasValue)
            {
                var (foodId, alias) = exact.Value;
                return (foodId, 1.0m, alias);
            }

            // 2) Exact canonical name (Foods.Name)
            // NOT: Normalize(x.Name) EF tarafında translate olmaz, o yüzden önce çekip sonra normalize ediyoruz.
            // Küçük DB’de sorun değil. Büyük olursa ayrı normalize kolonuna geçeriz.
            var foods = await _db.Foods
                .AsNoTracking()
                .Select(f => new { f.Id, f.Name })
                .ToListAsync(ct);

            var exactFood = foods.FirstOrDefault(x => FoodTextNormalizer.Normalize(x.Name) == norm);
            if (exactFood != null)
                return (exactFood.Id, 0.95m, exactFood.Name);

            // 3) Fuzzy alias match (lang then en)
            var fuzzy = await FindBestFuzzyAliasAsync(norm, lang, ct)
                       ?? await FindBestFuzzyAliasAsync(norm, "en", ct);

            if (!fuzzy.HasValue)
                return (null, 0m, null);

            var (fFoodId, fAlias, fScore) = fuzzy.Value;

            if (fScore < 0.70m)
                return (null, 0m, null);

            return (fFoodId, fScore, fAlias);
        }

        private static string NormalizeLang(string? language)
        {
            var l = (language ?? "en").Trim().ToLowerInvariant();
            if (l.Contains('-')) l = l.Split('-')[0]; // tr-TR -> tr
            return string.IsNullOrWhiteSpace(l) ? "en" : l;
        }

        private async Task<(int foodId, string alias)?> FindExactAliasAsync(string norm, string lang, CancellationToken ct)
        {
            var hit = await _db.FoodAliases
                .AsNoTracking()
                .Where(a => a.Language == lang && a.NormalizedAlias == norm)
                .Select(a => new { a.FoodId, a.Alias })
                .FirstOrDefaultAsync(ct);

            return hit == null ? null : (hit.FoodId, hit.Alias);
        }

        private async Task<(int foodId, string alias, decimal score)?> FindBestFuzzyAliasAsync(
            string norm,
            string lang,
            CancellationToken ct)
        {
            var prefix = norm.Length >= 3 ? norm.Substring(0, 3) : norm;

            var candidates = await _db.FoodAliases
                .AsNoTracking()
                .Where(a =>
                    a.Language == lang &&
                    (a.NormalizedAlias.StartsWith(prefix) ||
                     a.NormalizedAlias.Contains(norm) ||
                     norm.Contains(a.NormalizedAlias)))
                .Select(a => new { a.FoodId, a.Alias, a.NormalizedAlias })
                .Take(200)
                .ToListAsync(ct);

            if (candidates.Count == 0)
                return null;

            int bestFoodId = 0;
            string bestAlias = "";
            decimal bestScore = -1m;

            foreach (var c in candidates)
            {
                var score = Similarity(norm, c.NormalizedAlias);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestFoodId = c.FoodId;
                    bestAlias = c.Alias;
                }
            }

            return (bestFoodId, bestAlias, bestScore);
        }

        private static decimal Similarity(string a, string b)
        {
            if (a == b) return 1m;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0m;

            var dist = LevenshteinDistance(a, b);
            var maxLen = Math.Max(a.Length, b.Length);
            if (maxLen == 0) return 1m;

            var score = 1m - (decimal)dist / maxLen;

            if (b.Contains(a) || a.Contains(b))
                score = Math.Min(1m, score + 0.10m);

            return score;
        }

        private static int LevenshteinDistance(string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[n, m];
        }
    }
}