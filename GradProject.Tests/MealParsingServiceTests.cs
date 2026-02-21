using System.Net.Http;
using GradProject.Application.DTOs.Nutrition.AI;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services.Nutrition.AI;
using Xunit;

namespace GradProject.Tests;

public class MealParsingServiceTests
{
    // 🔥 Fake Resolver (DI hatasını çözer)
    private class FakeFoodCanonicalResolver : IFoodCanonicalResolver
    {
        public Task<(int? foodId, decimal confidence, string? matchedAlias)> ResolveAsync(
            string input,
            string language,
            CancellationToken ct = default)
        {
            return Task.FromResult<(int?, decimal, string?)>((null, 0m, null));
        }
    }

    private MealParsingService CreateService(AppDbContext db)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8000") };
        var resolver = new FakeFoodCanonicalResolver();
        return new MealParsingService(db, httpClient, resolver);
    }

    [Fact]
    public async Task ParseAsync_ParsesCommaSeparatedFoods_UsesDefaultPortionForCount_NoUnit()
    {
        using var db = TestDbFactory.Create(nameof(ParseAsync_ParsesCommaSeparatedFoods_UsesDefaultPortionForCount_NoUnit));

        var egg = new Food
        {
            Id = 1,
            Name = "egg",
            Category = "protein",
            Source = "USDA",
            DefaultPortionG = 50m,
            Kcal = 140m,
            ProteinG = 13m,
            FatG = 10m,
            CarbG = 1m,
            SugarG = 0m,
            FiberG = 0m,
            SodiumMg = 0m
        };

        var milk = new Food
        {
            Id = 2,
            Name = "milk",
            Category = "dairy",
            Source = "USDA",
            DefaultPortionG = 100m,
            Kcal = 60m,
            ProteinG = 3.2m,
            FatG = 3.3m,
            CarbG = 4.8m,
            SugarG = 4.8m,
            FiberG = 0m,
            SodiumMg = 44m
        };

        db.Foods.AddRange(egg, milk);
        await db.SaveChangesAsync();

        db.FoodAliases.AddRange(
            new FoodAlias
            {
                FoodId = egg.Id,
                Language = "tr",
                Alias = "yumurta",
                NormalizedAlias = "yumurta"
            },
            new FoodAlias
            {
                FoodId = milk.Id,
                Language = "tr",
                Alias = "sut",
                NormalizedAlias = "sut"
            }
        );

        await db.SaveChangesAsync();

        var svc = CreateService(db);

        var result = await svc.ParseAsync(123, new MealParseRequestDto
        {
            Text = "2 egg, 200 ml milk"
        });

        Assert.Equal(2, result.Items.Count);

        Assert.Equal(1, result.Items[0].MatchedFoodId);
        Assert.Equal(100m, result.Items[0].PortionG);

        Assert.Equal(2, result.Items[1].MatchedFoodId);
        Assert.Equal(200m, result.Items[1].PortionG);

        Assert.Equal(260m, Math.Round(result.TotalKcal, 2));
    }

    [Fact]
    public async Task ParseAsync_ParsesDecimalCommaAndKg_ConvertsToGrams()
    {
        using var db = TestDbFactory.Create(nameof(ParseAsync_ParsesDecimalCommaAndKg_ConvertsToGrams));

        var chicken = new Food
        {
            Id = 10,
            Name = "chicken",
            Category = "meat",
            Source = "USDA",
            DefaultPortionG = 100m,
            Kcal = 200m,
            ProteinG = 30m,
            FatG = 8m,
            CarbG = 0m,
            SugarG = 0m,
            FiberG = 0m,
            SodiumMg = 60m
        };

        db.Foods.Add(chicken);
        await db.SaveChangesAsync();

        var svc = CreateService(db);

        var result = await svc.ParseAsync(1, new MealParseRequestDto
        {
            Text = "1,5 kg chicken"
        });

        Assert.Single(result.Items);
        Assert.Equal(10, result.Items[0].MatchedFoodId);
        Assert.Equal(1500m, result.Items[0].PortionG);
        Assert.Equal(3000m, Math.Round(result.TotalKcal, 2));
    }

    [Fact]
    public async Task ParseAsync_UnknownFood_ReturnsUnmatchedItem_WithLowConfidence()
    {
        using var db = TestDbFactory.Create(nameof(ParseAsync_UnknownFood_ReturnsUnmatchedItem_WithLowConfidence));

        var svc = CreateService(db);

        var result = await svc.ParseAsync(1, new MealParseRequestDto
        {
            Text = "3 mysteryfood"
        });

        Assert.Single(result.Items);
        Assert.Null(result.Items[0].MatchedFoodId);
        Assert.False(result.Items[0].Kcal.HasValue);
    }

    [Fact]
    public async Task ParseAsync_AliasMatch_Works()
    {
        using var db = TestDbFactory.Create(nameof(ParseAsync_AliasMatch_Works));

        var egg = new Food
        {
            Id = 5,
            Name = "egg",
            Category = "protein",
            Source = "USDA",
            DefaultPortionG = 50m,
            Kcal = 140m,
            ProteinG = 13m,
            FatG = 10m,
            CarbG = 1m,
            SugarG = 0m,
            FiberG = 0m,
            SodiumMg = 0m
        };

        db.Foods.Add(egg);
        await db.SaveChangesAsync();

        db.FoodAliases.Add(new FoodAlias
        {
            FoodId = egg.Id,
            Language = "tr",
            Alias = "yumurta",
            NormalizedAlias = "yumurta"
        });

        await db.SaveChangesAsync();

        var svc = CreateService(db);

        var result = await svc.ParseAsync(1, new MealParseRequestDto
        {
            Text = "2 yumurta",
            Language = "tr"
        });

        Assert.Single(result.Items);
        Assert.Equal(5, result.Items[0].MatchedFoodId);
        Assert.Equal(100m, result.Items[0].PortionG);
    }
}