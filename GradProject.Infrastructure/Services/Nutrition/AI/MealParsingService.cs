using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GradProject.Application.DTOs.Nutrition.AI;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Application.Interfaces.Nutrition.AI;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Nutrition.AI
{
    public class MealParsingService : IMealParsingService
    {
        private readonly AppDbContext _db;
        private readonly HttpClient _httpClient;
        private readonly IFoodCanonicalResolver _canonicalResolver;

        public MealParsingService(AppDbContext db, HttpClient httpClient, IFoodCanonicalResolver canonicalResolver)
        {
            _db = db;
            _httpClient = httpClient;
            _canonicalResolver = canonicalResolver;
        }

        public async Task<MealParseResultDto> ParseAsync(int userId, MealParseRequestDto request, CancellationToken ct = default)
        {
            var payload = new { text = request.Text, consumedAt = request.ConsumedAt };

            PythonMealResponse? aiResponse;

            try
            {
                var response = await _httpClient.PostAsJsonAsync("/parse-meal", payload, ct);
                response.EnsureSuccessStatusCode();

                aiResponse = await response.Content.ReadFromJsonAsync<PythonMealResponse>(cancellationToken: ct);
            }
            catch
            {
                // AI servis kapalýysa þu an testte olduðu gibi patlamasýn:
                return new MealParseResultDto
                {
                    OriginalText = request.Text,
                    ConsumedAt = request.ConsumedAt,
                    Items = new List<MealParseItemDto>()
                };
            }

            if (aiResponse == null) return new MealParseResultDto();

            // AI sonucu DTO
            var result = new MealParseResultDto
            {
                OriginalText = aiResponse.OriginalText,
                ConsumedAt = aiResponse.ConsumedAt,
                TotalKcal = (decimal)aiResponse.TotalCalories,
                TotalProteinG = (decimal)aiResponse.TotalProtein,
                TotalFatG = (decimal)aiResponse.TotalFat,
                TotalCarbG = (decimal)aiResponse.TotalCarbs,
                Items = aiResponse.Items.Select(i => new MealParseItemDto
                {
                    Raw = i.Raw,
                    NormalizedName = i.NormalizedName,
                    MatchedFoodId = i.MatchedFoodId,
                    MatchedFoodName = i.MatchedFoodName,
                    Confidence = (decimal)i.Confidence,
                    PortionG = (decimal)i.PortionG,
                    Kcal = (decimal)i.Calories,
                    ProteinG = (decimal)i.Protein,
                    FatG = (decimal)i.Fat,
                    CarbG = (decimal)i.Carbs
                }).ToList()
            };

            //   Fallback: AI match yoksa/çok düþükse canonical resolver dene
            var lang = request.Language ?? "en"; // MealParseRequestDto'da yoksa eklemeye gerek yok; sabit "en" de geçilir.
            foreach (var item in result.Items)
            {
                var conf = item.Confidence;

                if (item.MatchedFoodId.HasValue && conf >= 0.80m)
                    continue;

                var textToResolve =
                    !string.IsNullOrWhiteSpace(item.NormalizedName) ? item.NormalizedName! :
                    !string.IsNullOrWhiteSpace(item.Raw) ? item.Raw! :
                    string.Empty;

                var (foodId, resolverConf, matchedAlias) =
                    await _canonicalResolver.ResolveAsync(textToResolve, lang, ct);

                if (!foodId.HasValue)
                    continue;

                item.MatchedFoodId = foodId;

                // Name'i db'den çek
                item.MatchedFoodName = await _db.Foods
                    .AsNoTracking()
                    .Where(f => f.Id == foodId.Value)
                    .Select(f => f.Name)
                    .FirstOrDefaultAsync(ct);

                // confidence'i iyileþtir (AI yoksa resolver'dan al)
                item.Confidence = Math.Max(conf, resolverConf);
            }

            return result;
        }

        public async Task SyncFoodsToAiAsync(CancellationToken ct = default)
        {
            var foods = await _db.Foods.AsNoTracking().ToListAsync(ct);

            var foodIds = foods.Select(f => f.Id).ToList();

            var aliasMap = await _db.FoodAliases
                .AsNoTracking()
                .Where(a => foodIds.Contains(a.FoodId))
                .GroupBy(a => a.FoodId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(x => x.Alias).Distinct().ToArray(),
                    ct);

            var payload = new
            {
                foods = foods.Select(f => new
                {
                    id = f.Id,
                    name = f.Name,
                    aliases = aliasMap.TryGetValue(f.Id, out var arr)
                        ? arr
                        : Array.Empty<string>(),
                    default_portion_g = f.DefaultPortionG,
                    calories_per_100g = f.Kcal,
                    protein_per_100g = f.ProteinG,
                    carbs_per_100g = f.CarbG,
                    fat_per_100g = f.FatG
                }).ToList()
            };

            var response = await _httpClient.PostAsJsonAsync("/load-foods", payload, ct);
            response.EnsureSuccessStatusCode();
        }

        private class PythonMealResponse
        {
            [JsonPropertyName("originalText")]
            public string OriginalText { get; set; } = "";

            [JsonPropertyName("consumedAt")]
            public DateTime? ConsumedAt { get; set; }

            [JsonPropertyName("items")]
            public List<PythonParsedItem> Items { get; set; } = new();

            [JsonPropertyName("totalCalories")]
            public double TotalCalories { get; set; }
            [JsonPropertyName("totalProtein")]
            public double TotalProtein { get; set; }
            [JsonPropertyName("totalCarbs")]
            public double TotalCarbs { get; set; }
            [JsonPropertyName("totalFat")]
            public double TotalFat { get; set; }
        }

        private class PythonParsedItem
        {
            [JsonPropertyName("raw")]
            public string Raw { get; set; } = "";
            [JsonPropertyName("normalizedName")]
            public string NormalizedName { get; set; } = "";
            [JsonPropertyName("matchedFoodId")]
            public int? MatchedFoodId { get; set; }
            [JsonPropertyName("matchedFoodName")]
            public string? MatchedFoodName { get; set; }
            [JsonPropertyName("confidence")]
            public double Confidence { get; set; }
            [JsonPropertyName("portionG")]
            public double PortionG { get; set; }
            [JsonPropertyName("calories")]
            public double Calories { get; set; }
            [JsonPropertyName("protein")]
            public double Protein { get; set; }
            [JsonPropertyName("carbs")]
            public double Carbs { get; set; }
            [JsonPropertyName("fat")]
            public double Fat { get; set; }
        }
    }
}