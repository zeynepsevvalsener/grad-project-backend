using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GradProject.Application.DTOs.Nutrition.AI;
using GradProject.Application.Interfaces.Nutrition.AI;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Nutrition.AI
{
    public class MealParsingService : IMealParsingService
    {
        private readonly AppDbContext _db;
        private readonly HttpClient _httpClient;

        public MealParsingService(AppDbContext db, HttpClient httpClient)
        {
            _db = db;
            _httpClient = httpClient;
        }

        public async Task<MealParseResultDto> ParseAsync(int userId, MealParseRequestDto request, CancellationToken ct = default)
        {
            var payload = new { text = request.Text, consumedAt = request.ConsumedAt };

            var response = await _httpClient.PostAsJsonAsync("/parse-meal", payload, ct);
            response.EnsureSuccessStatusCode();

            var aiResponse = await response.Content.ReadFromJsonAsync<PythonMealResponse>(cancellationToken: ct);
            if (aiResponse == null) return new MealParseResultDto();

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

            return result;
        }

        public async Task SyncFoodsToAiAsync(CancellationToken ct = default)
        {
            var foods = await _db.Foods.AsNoTracking().ToListAsync(ct);

            var payload = new
            {
                foods = foods.Select(f => new
                {
                    id = f.Id,
                    name = f.Name,
                    aliases = f.Aliases ?? new string[0],
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