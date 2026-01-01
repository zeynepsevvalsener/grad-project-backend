using System.Globalization;
using System.Text.RegularExpressions;
using GradProject.Application.DTOs.Nutrition.AI;
using GradProject.Application.Interfaces.Nutrition.AI;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Nutrition.AI
{
    public class MealParsingService : IMealParsingService
    {
        private readonly AppDbContext _db;

        public MealParsingService(AppDbContext db)
        {
            _db = db;
        }

        private static readonly Dictionary<string, decimal> UnitToGramMultiplier = new(StringComparer.OrdinalIgnoreCase)
        {
            ["g"] = 1m,
            ["gr"] = 1m,
            ["gram"] = 1m,
            ["grams"] = 1m,

            ["kg"] = 1000m,
            ["kilogram"] = 1000m,
            ["kilograms"] = 1000m,

            ["ml"] = 1m,
            ["milliliter"] = 1m,
            ["milliliters"] = 1m,

            ["l"] = 1000m,
            ["lt"] = 1000m,
            ["liter"] = 1000m,
            ["liters"] = 1000m,
        };

        private static readonly HashSet<string> CountUnits = new(StringComparer.OrdinalIgnoreCase)
        {
            "piece","pieces","pc","pcs",
            "slice","slices",
            "adet","tane"
        };

        private static readonly Regex QtyUnitNameRegex = new(
            @"^\s*(?<qty>\d+([.,]\d+)?)\s*(?<unit>[a-zA-ZçðýöþüÇÐÝÖÞÜ]+)?\s*(?<name>.+?)\s*$",
            RegexOptions.Compiled);

        private static readonly Regex SplitRegex = new(
            @"\s*(,|;|\+|\band\b|\bve\b)\s*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public async Task<MealParseResultDto> ParseAsync(int userId, MealParseRequestDto request, CancellationToken ct = default)
        {
            var text = (request.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return new MealParseResultDto
                {
                    OriginalText = request.Text ?? string.Empty,
                    ConsumedAt = request.ConsumedAt
                };
            }

            var parts = SplitRegex.Split(text)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Where(x => x is not "," and not ";" and not "+")
                .ToList();

            var result = new MealParseResultDto
            {
                OriginalText = text,
                ConsumedAt = request.ConsumedAt
            };

            foreach (var part in parts)
            {
                var item = await ParseOneAsync(part, ct);
                result.Items.Add(item);

                if (item.Kcal.HasValue) result.TotalKcal += item.Kcal.Value;
                if (item.ProteinG.HasValue) result.TotalProteinG += item.ProteinG.Value;
                if (item.FatG.HasValue) result.TotalFatG += item.FatG.Value;
                if (item.CarbG.HasValue) result.TotalCarbG += item.CarbG.Value;
            }

            result.TotalKcal = Round2(result.TotalKcal);
            result.TotalProteinG = Round2(result.TotalProteinG);
            result.TotalFatG = Round2(result.TotalFatG);
            result.TotalCarbG = Round2(result.TotalCarbG);

            return result;
        }

        private async Task<MealParseItemDto> ParseOneAsync(string raw, CancellationToken ct)
        {
            var trimmed = raw.Trim();

            trimmed = Regex.Replace(trimmed, @"\b(of|a|an|some)\b", "", RegexOptions.IgnoreCase).Trim();

            decimal qty = 1m;
            string? unit = null;
            string name = trimmed;

            var m = QtyUnitNameRegex.Match(trimmed);
            if (m.Success)
            {
                var qtyStr = m.Groups["qty"].Value.Replace(',', '.');
                if (decimal.TryParse(qtyStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedQty))
                    qty = parsedQty;

                unit = m.Groups["unit"].Success ? m.Groups["unit"].Value.Trim() : null;
                name = m.Groups["name"].Value.Trim();
            }

            var normalizedName = NormalizeFoodName(name);
            var matchedFood = await FindBestFoodMatchAsync(normalizedName, ct);

            var portionG = ComputePortionGrams(qty, unit, matchedFood?.DefaultPortionG);

            var dto = new MealParseItemDto
            {
                Raw = raw,
                NormalizedName = normalizedName,
                PortionG = portionG
            };

            if (matchedFood is null)
            {
                dto.Confidence = 0.10m;
                return dto;
            }

            dto.MatchedFoodId = matchedFood.Id;
            dto.MatchedFoodName = matchedFood.Name;
            dto.Confidence = ComputeConfidence(normalizedName, matchedFood);

            var factor = portionG / 100m;

            dto.Kcal = Round2(matchedFood.Kcal * factor);
            dto.ProteinG = Round2(matchedFood.ProteinG * factor);
            dto.FatG = Round2(matchedFood.FatG * factor);
            dto.CarbG = Round2(matchedFood.CarbG * factor);

            return dto;
        }

        private static string NormalizeFoodName(string name)
        {
            var n = name.Trim().ToLowerInvariant();

            n = Regex.Replace(n, @"[^\p{L}\p{N}\s]", " ");
            n = Regex.Replace(n, @"\s+", " ").Trim();

            return n;
        }

        private static decimal ComputePortionGrams(decimal qty, string? unit, decimal? defaultPortionG)
        {
            if (!string.IsNullOrWhiteSpace(unit))
            {
                if (UnitToGramMultiplier.TryGetValue(unit, out var mult))
                    return Round2(qty * mult);

                if (CountUnits.Contains(unit))
                {
                    var baseG = defaultPortionG ?? 100m;
                    return Round2(qty * baseG);
                }
            }

            if (defaultPortionG.HasValue)
                return Round2(qty * defaultPortionG.Value);

            return Round2(qty * 100m);
        }

        private static decimal ComputeConfidence(string normalizedName, Domain.Entities.Food matched)
        {
            if (string.Equals(matched.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
                return 1.0m;

            if (matched.Aliases != null &&
                matched.Aliases.Any(a => string.Equals(a, normalizedName, StringComparison.OrdinalIgnoreCase)))
                return 0.9m;

            if (matched.Name.Contains(normalizedName, StringComparison.OrdinalIgnoreCase) ||
                normalizedName.Contains(matched.Name, StringComparison.OrdinalIgnoreCase))
                return 0.7m;

            return 0.5m;
        }

        private async Task<Domain.Entities.Food?> FindBestFoodMatchAsync(string normalizedName, CancellationToken ct)
        {
            var exact = await _db.Foods.AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.Name.ToLower() == normalizedName ||
                    (f.Aliases != null && f.Aliases.Any(a => a.ToLower() == normalizedName)),
                    ct);

            if (exact != null) return exact;

            var pattern = $"%{normalizedName}%";
            var candidates = await _db.Foods.AsNoTracking()
                .Where(f =>
                    EF.Functions.ILike(f.Name, pattern) ||
                    (f.Aliases != null && f.Aliases.Any(a => EF.Functions.ILike(a, pattern))))
                .OrderBy(f => f.Name.Length)
                .Take(5)
                .ToListAsync(ct);

            return candidates.FirstOrDefault();
        }

        private static decimal Round2(decimal v) => Math.Round(v, 2);
    }
}
