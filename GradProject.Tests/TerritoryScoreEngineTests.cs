using GradProject.Application.Services.TerritoryScoring;

namespace GradProject.Tests;

public class TerritoryScoreEngineTests
{
    private static TerritoryScoreEngine CreateEngine() =>
        new TerritoryScoreEngine(new DefaultScoreFormula(), new DefaultRepeatCalculator());

    private static ScoringFormulaConfig DefaultConfig() => new()
    {
        DistanceWeight = 0.3,
        CoverageWeight = 0.3,
        PaceWeight = 0.2,
        CompletionWeight = 0.2,
        RepeatDecayFactor = 0.2,
        TerritoryWeightEnabled = false
    };

    [Fact]
    public void ComputeRunContributions_SameInput_ProducesSameOutput_Determinism()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1,
            UserId = 10,
            DistanceMeters = 5000,
            AveragePace = 300,
            CompletionDurationSeconds = 1500,
            CoveredCellIds = new[] { "C1", "C2", "C3", "C4", "C5" }
        };
        var territory = new TerritoryInput
        {
            Id = 20,
            TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10" }
        };
        var territories = new[] { territory };
        var userHistoryMap = new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>();
        var config = DefaultConfig();

        var first = engine.ComputeRunContributions(run, territories, userHistoryMap, config);
        var second = engine.ComputeRunContributions(run, territories, userHistoryMap, config);

        Assert.Equal(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].TerritoryId, second[i].TerritoryId);
            Assert.Equal(first[i].FinalScore, second[i].FinalScore);
            Assert.Equal(first[i].BaseScore, second[i].BaseScore);
            Assert.Equal(first[i].CoverageRatio, second[i].CoverageRatio);
        }
    }

    [Fact]
    public void ComputeRunContributions_ZeroCoverage_ReturnsEmptyOrSkipsTerritory()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1,
            UserId = 10,
            DistanceMeters = 5000,
            AveragePace = 300,
            CompletionDurationSeconds = 1500,
            CoveredCellIds = Array.Empty<string>()
        };
        var territory = new TerritoryInput
        {
            Id = 20,
            TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2" }
        };
        var territories = new[] { territory };
        var config = DefaultConfig();

        var contributions = engine.ComputeRunContributions(run, territories, new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>(), config);

        Assert.Empty(contributions);
    }

    [Fact]
    public void ComputeRunContributions_TotalCellCountZero_SkipsTerritory()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1,
            UserId = 10,
            DistanceMeters = 5000,
            CoveredCellIds = new[] { "C1" },
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 20, 1 } }
        };
        var territory = new TerritoryInput { Id = 20, TotalCellCount = 0 };
        var territories = new[] { territory };
        var config = DefaultConfig();

        var contributions = engine.ComputeRunContributions(run, territories, new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>(), config);

        Assert.Empty(contributions);
    }

    [Fact]
    public void RepeatEffect_ZeroPreviousCount_MultiplierIsOne()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1,
            UserId = 10,
            DistanceMeters = 5000,
            AveragePace = 300,
            CompletionDurationSeconds = 1500,
            CoveredCellIds = new[] { "C1", "C2" }
        };
        var territory = new TerritoryInput
        {
            Id = 20,
            TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10" }
        };
        var userHistoryMap = new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>();
        var config = DefaultConfig();

        var contributions = engine.ComputeRunContributions(run, new[] { territory }, userHistoryMap, config);

        Assert.Single(contributions);
        Assert.Equal(1.0, contributions[0].RepeatMultiplier);
    }

    [Fact]
    public void RepeatEffect_WithPreviousContributions_MultiplierDecreases()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1,
            UserId = 10,
            DistanceMeters = 5000,
            AveragePace = 300,
            CompletionDurationSeconds = 1500,
            CoveredCellIds = new[] { "C1", "C2" }
        };
        var territory = new TerritoryInput
        {
            Id = 20,
            TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10" }
        };
        var userHistoryMap = new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>
        {
            [10] = new Dictionary<int, UserTerritoryHistoryInput>
            {
                [20] = new UserTerritoryHistoryInput { UserId = 10, TerritoryId = 20, PreviousContributionCount = 2 }
            }
        };
        var config = DefaultConfig();

        var contributions = engine.ComputeRunContributions(run, new[] { territory }, userHistoryMap, config);

        Assert.Single(contributions);
        double expectedMultiplier = 1.0 / (1.0 + 2 * 0.2);
        Assert.Equal(Math.Round(expectedMultiplier, TerritoryScoreEngine.ScoreDecimalPlaces), contributions[0].RepeatMultiplier);
        Assert.True(contributions[0].RepeatMultiplier < 1.0);
    }

    [Fact]
    public void ConfigChange_DifferentWeights_ScoreChanges()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1,
            UserId = 10,
            DistanceMeters = 10000,
            AveragePace = 300,
            CompletionDurationSeconds = 1500,
            CoveredCellIds = new[] { "C1", "C2", "C3", "C4", "C5" }
        };
        var territory = new TerritoryInput
        {
            Id = 20,
            TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10" }
        };
        var territories = new[] { territory };

        var configLowDistance = new ScoringFormulaConfig
        {
            DistanceWeight = 0.1,
            CoverageWeight = 0.4,
            PaceWeight = 0.25,
            CompletionWeight = 0.25,
            RepeatDecayFactor = 0.2,
            TerritoryWeightEnabled = false
        };
        var configHighDistance = new ScoringFormulaConfig
        {
            DistanceWeight = 0.5,
            CoverageWeight = 0.2,
            PaceWeight = 0.15,
            CompletionWeight = 0.15,
            RepeatDecayFactor = 0.2,
            TerritoryWeightEnabled = false
        };

        var emptyHistory = new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>();
        var contribLow = engine.ComputeRunContributions(run, territories, emptyHistory, configLowDistance);
        var contribHigh = engine.ComputeRunContributions(run, territories, emptyHistory, configHighDistance);

        Assert.Single(contribLow);
        Assert.Single(contribHigh);
        Assert.NotEqual(contribLow[0].BaseScore, contribHigh[0].BaseScore);
    }

    [Fact]
    public void AggregateUserTerritoryScore_EmptyContributions_ReturnsZeros()
    {
        var engine = CreateEngine();
        var aggregate = engine.AggregateUserTerritoryScore(Array.Empty<TerritoryContribution>());

        Assert.Equal(0, aggregate.TotalTerritoryScore);
        Assert.Equal(0, aggregate.TotalDistance);
        Assert.Equal(0, aggregate.AveragePace);
        Assert.Equal(0, aggregate.TerritoryCount);
        Assert.Equal(0, aggregate.StrongestTerritoryScore);
    }

    [Fact]
    public void AggregateUserTerritoryScore_WithContributions_SumsAndComputesPace()
    {
        var engine = CreateEngine();
        var contributions = new List<TerritoryContribution>
        {
            new() { TerritoryId = 1, FinalScore = 0.5, DistanceInTerritory = 1000, AveragePace = 300 },
            new() { TerritoryId = 2, FinalScore = 0.3, DistanceInTerritory = 2000, AveragePace = 360 }
        };
        var aggregate = engine.AggregateUserTerritoryScore(contributions);

        Assert.Equal(0.8, aggregate.TotalTerritoryScore);
        Assert.Equal(3000, aggregate.TotalDistance);
        Assert.Equal(2, aggregate.TerritoryCount);
        Assert.Equal(0.5, aggregate.StrongestTerritoryScore);
        double expectedPace = (1000 * 300 + 2000 * 360) / 3000;
        Assert.Equal(Math.Round(expectedPace, TerritoryScoreEngine.ScoreDecimalPlaces), aggregate.AveragePace);
    }

    [Fact]
    public void ComputeRunContributions_UsesPerTerritoryCoveredCount_WhenProvided()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1,
            UserId = 10,
            DistanceMeters = 5000,
            AveragePace = 300,
            CompletionDurationSeconds = 1500,
            CoveredCellIds = Array.Empty<string>(),
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 20, 5 } }
        };
        var territory = new TerritoryInput { Id = 20, TotalCellCount = 10 };
        var config = DefaultConfig();

        var contributions = engine.ComputeRunContributions(run, new[] { territory }, new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>(), config);

        Assert.Single(contributions);
        Assert.Equal(0.5, contributions[0].CoverageRatio);
        Assert.Equal(20, contributions[0].TerritoryId);
    }

    [Fact]
    public void StrategySwap_DifferentFormula_ProducesDifferentScore()
    {
        var defaultEngine = CreateEngine();
        var fixedFormula = new FixedScoreFormula(0.25);
        var customEngine = new TerritoryScoreEngine(fixedFormula, new DefaultRepeatCalculator());

        var run = new RunInput
        {
            Id = 1,
            UserId = 10,
            DistanceMeters = 5000,
            AveragePace = 300,
            CompletionDurationSeconds = 1500,
            CoveredCellIds = new[] { "C1" }
        };
        var territory = new TerritoryInput
        {
            Id = 20,
            TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10" }
        };
        var config = DefaultConfig();

        var emptyHistory = new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>();
        var defaultContrib = defaultEngine.ComputeRunContributions(run, new[] { territory }, emptyHistory, config);
        var customContrib = customEngine.ComputeRunContributions(run, new[] { territory }, emptyHistory, config);

        Assert.Single(defaultContrib);
        Assert.Single(customContrib);
        Assert.NotEqual(defaultContrib[0].BaseScore, customContrib[0].BaseScore);
        Assert.Equal(0.25, customContrib[0].BaseScore);
    }

    /// <summary>
    /// Manuel test: Çalıştırınca örnek Run → Contributions → Aggregate çıktısı verir.
    /// Görüntülemek için: dotnet test --filter "ManualTest_PrintSampleScenario" --logger "console;verbosity=detailed"
    /// </summary>
    [Fact]
    public void ManualTest_PrintSampleScenario()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1,
            UserId = 100,
            DistanceMeters = 5000,
            AveragePace = 300,
            CompletionDurationSeconds = 1500,
            CoveredCellIds = new[] { "C1", "C2", "C3", "C4", "C5" }
        };
        var territory = new TerritoryInput
        {
            Id = 10,
            TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10" }
        };
        var userHistory = new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>
        {
            [100] = new Dictionary<int, UserTerritoryHistoryInput>
            {
                [10] = new UserTerritoryHistoryInput { UserId = 100, TerritoryId = 10, PreviousContributionCount = 2 }
            }
        };
        var config = DefaultConfig();

        var contributions = engine.ComputeRunContributions(run, new[] { territory }, userHistory, config);
        var aggregate = engine.AggregateUserTerritoryScore(contributions);

        Console.WriteLine("=== Territory Score Engine - Örnek Senaryo ===");
        Console.WriteLine("Run: UserId={0}, Distance={1}m, Pace={2} s/km, CoveredCells={3}", run.UserId, run.DistanceMeters, run.AveragePace, run.CoveredCellIds.Count);
        Console.WriteLine("Territory: Id={0}, TotalCells={1}", territory.Id, territory.TotalCellCount);
        Console.WriteLine("History: previousContributionCount=2 → repeat decay uygulanır");
        Console.WriteLine();
        foreach (var c in contributions)
        {
            Console.WriteLine("Contribution: TerritoryId={0}, CoverageRatio={1}, BaseScore={2}, RepeatMult={3}, FinalScore={4}, DistanceInTerritory={5}",
                c.TerritoryId, c.CoverageRatio, c.BaseScore, c.RepeatMultiplier, c.FinalScore, c.DistanceInTerritory);
        }
        Console.WriteLine();
        Console.WriteLine("Aggregate: TotalScore={0}, TotalDistance={1}, AvgPace={2}, TerritoryCount={3}, StrongestScore={4}",
            aggregate.TotalTerritoryScore, aggregate.TotalDistance, aggregate.AveragePace, aggregate.TerritoryCount, aggregate.StrongestTerritoryScore);
        Console.WriteLine("=== Son ===");

        Assert.True(contributions.Count > 0);
        Assert.True(aggregate.TotalTerritoryScore > 0);
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var engine = CreateEngine();
        var run = new RunInput { Id = 1, UserId = 1, CoveredCellIds = Array.Empty<string>() };
        var territories = new List<TerritoryInput>();
        var config = DefaultConfig();

        var emptyHistory = new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>();
        Assert.Throws<ArgumentNullException>(() => engine.ComputeRunContributions(null!, territories, emptyHistory, config));
        Assert.Throws<ArgumentNullException>(() => engine.ComputeRunContributions(run, null!, emptyHistory, config));
        Assert.Throws<ArgumentNullException>(() => engine.ComputeRunContributions(run, territories, emptyHistory, null!));
    }

    private sealed class FixedScoreFormula : ITerritoryScoreFormula
    {
        private readonly double _fixedScore;
        public FixedScoreFormula(double fixedScore) => _fixedScore = fixedScore;
        public double ComputeBaseScore(RunInput run, double coverageRatio, TerritoryInput territory, ScoringFormulaConfig config) => _fixedScore;
    }
}
