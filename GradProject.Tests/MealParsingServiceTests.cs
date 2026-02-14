using System.Net.Http;
using GradProject.Application.DTOs.Nutrition.AI;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services.Nutrition.AI;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GradProject.Tests;

public class MealParsingServiceTests
{
    [Fact]
    public async Task ParseAsync_ParsesCommaSeparatedFoods_UsesDefaultPortionForCount_NoUnit()
    {
        using var db = TestDbFactory.Create(nameof(ParseAsync_ParsesCommaSeparatedFoods_UsesDefaultPortionForCount_NoUnit));

        db.Foods.AddRange(
            new Food
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
                SodiumMg = 0m,
                Aliases = new[] { "yumurta" }
            },
            new Food
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
                SodiumMg = 44m,
                Aliases = new[] { "s�t", "sut" }
            }
        );
        await db.SaveChangesAsync();

        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8000") };
        var svc = new MealParsingService(db, httpClient);

        var request = new MealParseRequestDto
        {
            Text = "2 egg, 200 ml milk"
        };

        var result = await svc.ParseAsync(userId: 123, request);

        Assert.Equal(2, result.Items.Count);

        var eggItem = result.Items[0];
        Assert.Equal("egg", eggItem.NormalizedName);
        Assert.Equal(1, eggItem.MatchedFoodId);
        Assert.Equal(100m, eggItem.PortionG);
        Assert.True(eggItem.Kcal.HasValue);
        Assert.Equal(140m, Math.Round(eggItem.Kcal.Value, 2));

        var milkItem = result.Items[1];
        Assert.Equal("milk", milkItem.NormalizedName);
        Assert.Equal(2, milkItem.MatchedFoodId);
        Assert.Equal(200m, milkItem.PortionG);
        Assert.True(milkItem.Kcal.HasValue);
        Assert.Equal(120m, Math.Round(milkItem.Kcal.Value, 2));

        Assert.Equal(260m, Math.Round(result.TotalKcal, 2));
    }

    [Fact]
    public async Task ParseAsync_ParsesDecimalCommaAndKg_ConvertsToGrams()
    {
        using var db = TestDbFactory.Create(nameof(ParseAsync_ParsesDecimalCommaAndKg_ConvertsToGrams));

        db.Foods.Add(new Food
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
            SodiumMg = 60m,
            Aliases = Array.Empty<string>()
        });
        await db.SaveChangesAsync();

        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8000") };
        var svc = new MealParsingService(db, httpClient);

        var result = await svc.ParseAsync(1, new MealParseRequestDto { Text = "1,5 kg chicken" });

        Assert.Single(result.Items);
        var item = result.Items[0];

        Assert.Equal("chicken", item.NormalizedName);
        Assert.Equal(10, item.MatchedFoodId);

        Assert.Equal(1500m, item.PortionG);
        Assert.True(item.Kcal.HasValue);

        Assert.Equal(3000m, Math.Round(item.Kcal.Value, 2));
    }

    [Fact]
    public async Task ParseAsync_UnknownFood_ReturnsUnmatchedItem_WithLowConfidence()
    {
        using var db = TestDbFactory.Create(nameof(ParseAsync_UnknownFood_ReturnsUnmatchedItem_WithLowConfidence));
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8000") };
        var svc = new MealParsingService(db, httpClient);

        var result = await svc.ParseAsync(1, new MealParseRequestDto { Text = "3 mysteryfood" });

        Assert.Single(result.Items);
        var item = result.Items[0];

        Assert.Null(item.MatchedFoodId);
        Assert.False(item.Kcal.HasValue);
        Assert.True(item.Confidence <= 0.2m);
        Assert.Equal(300m, item.PortionG);
    }

    [Fact]
    public async Task ParseAsync_AliasMatch_Works()
    {
        using var db = TestDbFactory.Create(nameof(ParseAsync_AliasMatch_Works));

        db.Foods.Add(new Food
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
            SodiumMg = 0m,
            Aliases = new[] { "yumurta" }
        });
        await db.SaveChangesAsync();

        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8000") };
        var svc = new MealParsingService(db, httpClient);

        var result = await svc.ParseAsync(1, new MealParseRequestDto { Text = "2 yumurta" });

        Assert.Single(result.Items);
        var item = result.Items[0];

        Assert.Equal(5, item.MatchedFoodId);
        Assert.Equal(100m, item.PortionG);
        Assert.True(item.Confidence >= 0.8m);
    }
}
