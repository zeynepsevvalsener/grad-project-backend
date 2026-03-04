using GradProject.Application.Services.TerritoryScoring;

namespace GradProject.Tests;

public class TerritoryScoreEngineTests
{
    private static TerritoryScoreEngine CreateEngine() =>
        new(new DefaultScoreFormula(), new DefaultRepeatCalculator());

    private static ScoringFormulaConfig DefaultConfig() => new()
    {
        DistanceWeight = 0.3,
        CoverageWeight = 0.3,
        PaceWeight = 0.2,
        CompletionWeight = 0.2,
        CoverageExponent = 1.0,
        RepeatDecayFactor = 0.2,
        TerritoryWeightEnabled = false
    };

    private static Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>> EmptyHistory() => new();

    private static IReadOnlyList<TerritoryContribution> Compute(
        TerritoryScoreEngine engine,
        RunInput run,
        TerritoryInput territory,
        ScoringFormulaConfig config,
        Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>? history = null)
    {
        return engine.ComputeRunContributions(run, new[] { territory }, history ?? EmptyHistory(), config);
    }

    /// <summary>Reproduces the formula locally so tests assert exact expected values.</summary>
    private static double ExpectedBaseScore(
        double distanceMeters, double coverageRatio, double paceSecPerKm, int completionSeconds,
        ScoringFormulaConfig config)
    {
        double d = Math.Min(1.0, distanceMeters / 20_000);
        double c = Math.Pow(Math.Clamp(coverageRatio, 0, 1), config.CoverageExponent);
        double p = Math.Clamp((600 - paceSecPerKm) / (600 - 180), 0, 1);
        double t = completionSeconds <= 0 ? 1.0
                 : completionSeconds >= 7200 ? 0.0
                 : 1.0 - completionSeconds / 7200.0;

        return config.DistanceWeight * d
             + config.CoverageWeight * c
             + config.PaceWeight * p
             + config.CompletionWeight * t;
    }

    // ── Determinism ──────────────────────────────────────────────────

    [Fact]
    public void ComputeRunContributions_SameInput_ProducesSameOutput_Determinism()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1, UserId = 10,
            DistanceMeters = 5000, AveragePace = 300, CompletionDurationSeconds = 1500,
            CoveredCellIds = new[] { "C1", "C2", "C3", "C4", "C5" }
        };
        var territory = new TerritoryInput
        {
            Id = 20, TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10" }
        };
        var config = DefaultConfig();

        var first = Compute(engine, run, territory, config);
        var second = Compute(engine, run, territory, config);

        Assert.Equal(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].TerritoryId, second[i].TerritoryId);
            Assert.Equal(first[i].FinalScore, second[i].FinalScore);
            Assert.Equal(first[i].BaseScore, second[i].BaseScore);
            Assert.Equal(first[i].CoverageRatio, second[i].CoverageRatio);
        }
    }

    // ── Edge cases ───────────────────────────────────────────────────

    [Fact]
    public void ComputeRunContributions_ZeroCoverage_ReturnsEmpty()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1, UserId = 10,
            DistanceMeters = 5000, AveragePace = 300, CompletionDurationSeconds = 1500,
            CoveredCellIds = Array.Empty<string>()
        };
        var territory = new TerritoryInput
        {
            Id = 20, TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2" }
        };

        var contributions = Compute(engine, run, territory, DefaultConfig());
        Assert.Empty(contributions);
    }

    [Fact]
    public void ComputeRunContributions_TotalCellCountZero_SkipsTerritory()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1, UserId = 10, DistanceMeters = 5000,
            CoveredCellIds = new[] { "C1" },
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 20, 1 } }
        };
        var territory = new TerritoryInput { Id = 20, TotalCellCount = 0 };

        var contributions = Compute(engine, run, territory, DefaultConfig());
        Assert.Empty(contributions);
    }

    // ── Repeat effect ────────────────────────────────────────────────

    [Fact]
    public void RepeatEffect_ZeroPreviousCount_MultiplierIsOne()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1, UserId = 10,
            DistanceMeters = 5000, AveragePace = 300, CompletionDurationSeconds = 1500,
            CoveredCellIds = new[] { "C1", "C2" }
        };
        var territory = new TerritoryInput
        {
            Id = 20, TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10" }
        };

        var contributions = Compute(engine, run, territory, DefaultConfig());
        Assert.Single(contributions);
        Assert.Equal(1.0, contributions[0].RepeatMultiplier);
    }

    [Fact]
    public void RepeatEffect_WithPreviousContributions_MultiplierDecreases()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1, UserId = 10,
            DistanceMeters = 5000, AveragePace = 300, CompletionDurationSeconds = 1500,
            CoveredCellIds = new[] { "C1", "C2" }
        };
        var territory = new TerritoryInput
        {
            Id = 20, TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10" }
        };
        var history = new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>
        {
            [10] = new Dictionary<int, UserTerritoryHistoryInput>
            {
                [20] = new() { UserId = 10, TerritoryId = 20, PreviousContributionCount = 2 }
            }
        };

        var contributions = Compute(engine, run, territory, DefaultConfig(), history);
        Assert.Single(contributions);

        double expectedMultiplier = 1.0 / (1.0 + 2 * 0.2);
        Assert.Equal(
            Math.Round(expectedMultiplier, TerritoryScoreEngine.ScoreDecimalPlaces),
            contributions[0].RepeatMultiplier);
        Assert.True(contributions[0].RepeatMultiplier < 1.0);
    }

    // ── Config weights ───────────────────────────────────────────────

    [Fact]
    public void ConfigChange_DifferentWeights_ScoreChanges()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1, UserId = 10,
            DistanceMeters = 10000, AveragePace = 300, CompletionDurationSeconds = 1500,
            CoveredCellIds = new[] { "C1", "C2", "C3", "C4", "C5" }
        };
        var territory = new TerritoryInput
        {
            Id = 20, TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10" }
        };

        var configLowDistance = DefaultConfig() with { DistanceWeight = 0.1, CoverageWeight = 0.4, PaceWeight = 0.25, CompletionWeight = 0.25 };
        var configHighDistance = DefaultConfig() with { DistanceWeight = 0.5, CoverageWeight = 0.2, PaceWeight = 0.15, CompletionWeight = 0.15 };

        var contribLow = Compute(engine, run, territory, configLowDistance);
        var contribHigh = Compute(engine, run, territory, configHighDistance);

        Assert.Single(contribLow);
        Assert.Single(contribHigh);
        Assert.NotEqual(contribLow[0].BaseScore, contribHigh[0].BaseScore);
    }

    // ── Aggregation ──────────────────────────────────────────────────

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

        double expectedPace = (1000 * 300 + 2000 * 360) / 3000.0;
        Assert.Equal(Math.Round(expectedPace, TerritoryScoreEngine.ScoreDecimalPlaces), aggregate.AveragePace);
    }

    // ── Per-territory count shortcut ─────────────────────────────────

    [Fact]
    public void ComputeRunContributions_UsesPerTerritoryCoveredCount_WhenProvided()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1, UserId = 10,
            DistanceMeters = 5000, AveragePace = 300, CompletionDurationSeconds = 1500,
            CoveredCellIds = Array.Empty<string>(),
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 20, 5 } }
        };
        var territory = new TerritoryInput { Id = 20, TotalCellCount = 10 };

        var contributions = Compute(engine, run, territory, DefaultConfig());
        Assert.Single(contributions);
        Assert.Equal(0.5, contributions[0].CoverageRatio);
        Assert.Equal(20, contributions[0].TerritoryId);
    }

    // ── Strategy swap ────────────────────────────────────────────────

    [Fact]
    public void StrategySwap_DifferentFormula_ProducesDifferentScore()
    {
        var defaultEngine = CreateEngine();
        var customEngine = new TerritoryScoreEngine(new FixedScoreFormula(0.25), new DefaultRepeatCalculator());

        var run = new RunInput
        {
            Id = 1, UserId = 10,
            DistanceMeters = 5000, AveragePace = 300, CompletionDurationSeconds = 1500,
            CoveredCellIds = new[] { "C1" }
        };
        var territory = new TerritoryInput
        {
            Id = 20, TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10" }
        };
        var config = DefaultConfig();

        var defaultContrib = Compute(defaultEngine, run, territory, config);
        var customContrib = Compute(customEngine, run, territory, config);

        Assert.Single(defaultContrib);
        Assert.Single(customContrib);
        Assert.NotEqual(defaultContrib[0].BaseScore, customContrib[0].BaseScore);
        Assert.Equal(0.25, customContrib[0].BaseScore);
    }

    // ── Numerical examples from design doc section 18 ────────────────

    [Fact]
    public void NumericalExample_Run1_FirstRunInTerritory()
    {
        var engine = CreateEngine();
        var config = DefaultConfig();
        var run = new RunInput
        {
            Id = 1, UserId = 100,
            DistanceMeters = 5000, AveragePace = 300, CompletionDurationSeconds = 1500,
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 10, 5 } }
        };
        var territory = new TerritoryInput { Id = 10, TotalCellCount = 10 };

        var contributions = Compute(engine, run, territory, config);
        Assert.Single(contributions);
        var c = contributions[0];

        double expected = ExpectedBaseScore(5000, 0.5, 300, 1500, config);
        Assert.Equal(Math.Round(expected, TerritoryScoreEngine.ScoreDecimalPlaces), c.BaseScore);
        Assert.Equal(1.0, c.RepeatMultiplier);
        Assert.Equal(c.BaseScore, c.FinalScore);
        Assert.Equal(0.5, c.CoverageRatio);
        Assert.Equal(2500.0, c.DistanceInTerritory);
    }

    [Fact]
    public void NumericalExample_Run2_ThirdContribution_RepeatDecay()
    {
        var engine = CreateEngine();
        var config = DefaultConfig();
        var run = new RunInput
        {
            Id = 2, UserId = 100,
            DistanceMeters = 5000, AveragePace = 300, CompletionDurationSeconds = 1500,
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 10, 5 } }
        };
        var territory = new TerritoryInput { Id = 10, TotalCellCount = 10 };
        var history = new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>
        {
            [100] = new Dictionary<int, UserTerritoryHistoryInput>
            {
                [10] = new() { UserId = 100, TerritoryId = 10, PreviousContributionCount = 2 }
            }
        };

        var c = Compute(engine, run, territory, config, history)[0];

        double expectedRepeat = Math.Round(1.0 / 1.4, TerritoryScoreEngine.ScoreDecimalPlaces);
        Assert.Equal(expectedRepeat, c.RepeatMultiplier);
        Assert.True(c.FinalScore < c.BaseScore, "Repeat decay should reduce FinalScore below BaseScore");
    }

    [Fact]
    public void NumericalExample_Run3_LongRun_HighCoverage()
    {
        var engine = CreateEngine();
        var config = DefaultConfig();
        var run = new RunInput
        {
            Id = 3, UserId = 200,
            DistanceMeters = 15000, AveragePace = 360, CompletionDurationSeconds = 3600,
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 20, 8 } }
        };
        var territory = new TerritoryInput { Id = 20, TotalCellCount = 10 };

        var c = Compute(engine, run, territory, config)[0];

        double expected = ExpectedBaseScore(15000, 0.8, 360, 3600, config);
        Assert.Equal(Math.Round(expected, TerritoryScoreEngine.ScoreDecimalPlaces), c.BaseScore);
        Assert.Equal(1.0, c.RepeatMultiplier);
        Assert.Equal(0.8, c.CoverageRatio);
        Assert.Equal(12000.0, c.DistanceInTerritory);
    }

    [Fact]
    public void NumericalExample_Aggregate_Run1PlusRun3()
    {
        var engine = CreateEngine();
        var config = DefaultConfig();

        var run1 = new RunInput
        {
            Id = 1, UserId = 100,
            DistanceMeters = 5000, AveragePace = 300, CompletionDurationSeconds = 1500,
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 10, 5 } }
        };
        var run3 = new RunInput
        {
            Id = 3, UserId = 100,
            DistanceMeters = 15000, AveragePace = 360, CompletionDurationSeconds = 3600,
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 20, 8 } }
        };
        var t1 = new TerritoryInput { Id = 10, TotalCellCount = 10 };
        var t2 = new TerritoryInput { Id = 20, TotalCellCount = 10 };

        var c1 = Compute(engine, run1, t1, config);
        var c3 = Compute(engine, run3, t2, config);
        var all = c1.Concat(c3).ToList();

        var agg = engine.AggregateUserTerritoryScore(all);

        Assert.Equal(2, agg.TerritoryCount);

        double expectedTotal = Math.Round(c1[0].FinalScore + c3[0].FinalScore, TerritoryScoreEngine.ScoreDecimalPlaces);
        Assert.Equal(expectedTotal, agg.TotalTerritoryScore);
        Assert.Equal(14500.0, agg.TotalDistance);
        Assert.Equal(c3[0].FinalScore, agg.StrongestTerritoryScore);

        double expectedPace = Math.Round((2500.0 * 300 + 12000.0 * 360) / 14500.0, TerritoryScoreEngine.ScoreDecimalPlaces);
        Assert.Equal(expectedPace, agg.AveragePace);
    }

    // ── Boundary tests ───────────────────────────────────────────────

    [Fact]
    public void Boundary_PaceAtMin_PNormIsOne()
    {
        var engine = CreateEngine();
        var config = DefaultConfig();
        var run = new RunInput
        {
            Id = 1, UserId = 1,
            DistanceMeters = 10000, AveragePace = 180, CompletionDurationSeconds = 1800,
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 1, 5 } }
        };
        var territory = new TerritoryInput { Id = 1, TotalCellCount = 10 };

        var c = Compute(engine, run, territory, config)[0];

        double expected = ExpectedBaseScore(10000, 0.5, 180, 1800, config);
        Assert.Equal(Math.Round(expected, TerritoryScoreEngine.ScoreDecimalPlaces), c.BaseScore);
    }

    [Fact]
    public void Boundary_PaceAtMax_PNormIsZero()
    {
        var engine = CreateEngine();
        var config = DefaultConfig();
        var run = new RunInput
        {
            Id = 1, UserId = 1,
            DistanceMeters = 10000, AveragePace = 600, CompletionDurationSeconds = 1800,
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 1, 5 } }
        };
        var territory = new TerritoryInput { Id = 1, TotalCellCount = 10 };

        var c = Compute(engine, run, territory, config)[0];

        double expected = ExpectedBaseScore(10000, 0.5, 600, 1800, config);
        Assert.Equal(Math.Round(expected, TerritoryScoreEngine.ScoreDecimalPlaces), c.BaseScore);
    }

    [Fact]
    public void Boundary_CompletionZero_TNormIsOne()
    {
        var engine = CreateEngine();
        var config = DefaultConfig();
        var run = new RunInput
        {
            Id = 1, UserId = 1,
            DistanceMeters = 10000, AveragePace = 300, CompletionDurationSeconds = 0,
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 1, 10 } }
        };
        var territory = new TerritoryInput { Id = 1, TotalCellCount = 10 };

        var c = Compute(engine, run, territory, config)[0];

        double expected = ExpectedBaseScore(10000, 1.0, 300, 0, config);
        Assert.Equal(Math.Round(expected, TerritoryScoreEngine.ScoreDecimalPlaces), c.BaseScore);
    }

    [Fact]
    public void Boundary_CompletionAtCap_TNormIsZero()
    {
        var engine = CreateEngine();
        var config = DefaultConfig();
        var run = new RunInput
        {
            Id = 1, UserId = 1,
            DistanceMeters = 10000, AveragePace = 300, CompletionDurationSeconds = 7200,
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 1, 10 } }
        };
        var territory = new TerritoryInput { Id = 1, TotalCellCount = 10 };

        var c = Compute(engine, run, territory, config)[0];

        double expected = ExpectedBaseScore(10000, 1.0, 300, 7200, config);
        Assert.Equal(Math.Round(expected, TerritoryScoreEngine.ScoreDecimalPlaces), c.BaseScore);
    }

    [Fact]
    public void Boundary_RepeatCountFive_StillPositive()
    {
        var calculator = new DefaultRepeatCalculator();
        double mult = calculator.GetMultiplier(5, 0.2);
        Assert.Equal(0.5, mult);
        Assert.True(mult > 0);
    }

    // ── Coverage exponent (alpha) ────────────────────────────────────

    [Fact]
    public void CoverageExponent_GreaterThanOne_RewardsHighCoverage()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1, UserId = 1,
            DistanceMeters = 10000, AveragePace = 300, CompletionDurationSeconds = 1800,
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 1, 9 } }
        };
        var territory = new TerritoryInput { Id = 1, TotalCellCount = 10 };

        var configAlpha1 = DefaultConfig() with { CoverageExponent = 1.0 };
        var configAlpha12 = DefaultConfig() with { CoverageExponent = 1.2 };

        var c1 = Compute(engine, run, territory, configAlpha1)[0];
        var c12 = Compute(engine, run, territory, configAlpha12)[0];

        Assert.True(c1.BaseScore > c12.BaseScore,
            "alpha=1.0 should give higher base score than alpha=1.2 for partial (90%) coverage");
    }

    [Fact]
    public void CoverageExponent_FullCoverage_NoEffectFromAlpha()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1, UserId = 1,
            DistanceMeters = 10000, AveragePace = 300, CompletionDurationSeconds = 1800,
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 1, 10 } }
        };
        var territory = new TerritoryInput { Id = 1, TotalCellCount = 10 };

        var configAlpha1 = DefaultConfig() with { CoverageExponent = 1.0 };
        var configAlpha2 = DefaultConfig() with { CoverageExponent = 2.0 };

        var c1 = Compute(engine, run, territory, configAlpha1)[0];
        var c2 = Compute(engine, run, territory, configAlpha2)[0];

        Assert.Equal(c1.BaseScore, c2.BaseScore);
    }

    // ── Territory weight ─────────────────────────────────────────────

    [Fact]
    public void TerritoryWeight_WhenEnabled_MultipliesFinalScore()
    {
        var engine = CreateEngine();
        var run = new RunInput
        {
            Id = 1, UserId = 1,
            DistanceMeters = 10000, AveragePace = 300, CompletionDurationSeconds = 1800,
            CoveredCellCountByTerritoryId = new Dictionary<int, int> { { 1, 5 } }
        };
        var territory = new TerritoryInput { Id = 1, TotalCellCount = 10, Weight = 1.5 };

        var configEnabled = DefaultConfig() with { TerritoryWeightEnabled = true };
        var configDisabled = DefaultConfig() with { TerritoryWeightEnabled = false };

        var cEnabled = Compute(engine, run, territory, configEnabled)[0];
        var cDisabled = Compute(engine, run, territory, configDisabled)[0];

        Assert.True(cEnabled.FinalScore > cDisabled.FinalScore,
            "Territory weight 1.5 should increase FinalScore");
        Assert.Equal(cEnabled.BaseScore, cDisabled.BaseScore);
    }

    // ── Null arguments ───────────────────────────────────────────────

    [Fact]
    public void NullArguments_Throw()
    {
        var engine = CreateEngine();
        var run = new RunInput { Id = 1, UserId = 1, CoveredCellIds = Array.Empty<string>() };
        var territories = new List<TerritoryInput>();
        var config = DefaultConfig();

        Assert.Throws<ArgumentNullException>(() => engine.ComputeRunContributions(null!, territories, EmptyHistory(), config));
        Assert.Throws<ArgumentNullException>(() => engine.ComputeRunContributions(run, null!, EmptyHistory(), config));
        Assert.Throws<ArgumentNullException>(() => engine.ComputeRunContributions(run, territories, EmptyHistory(), null!));
    }

    // ── End-to-end scenario ──────────────────────────────────────────

    [Fact]
    public void EndToEnd_MultipleRuns_AggregateIsCorrect()
    {
        var engine = CreateEngine();
        var config = DefaultConfig();
        var run = new RunInput
        {
            Id = 1, UserId = 100,
            DistanceMeters = 5000, AveragePace = 300, CompletionDurationSeconds = 1500,
            CoveredCellIds = new[] { "C1", "C2", "C3", "C4", "C5" }
        };
        var territory = new TerritoryInput
        {
            Id = 10, TotalCellCount = 10,
            CellIds = new HashSet<string> { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10" }
        };
        var history = new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>
        {
            [100] = new Dictionary<int, UserTerritoryHistoryInput>
            {
                [10] = new() { UserId = 100, TerritoryId = 10, PreviousContributionCount = 2 }
            }
        };

        var contributions = Compute(engine, run, territory, config, history);
        var aggregate = engine.AggregateUserTerritoryScore(contributions);

        Assert.True(contributions.Count > 0);
        Assert.True(aggregate.TotalTerritoryScore > 0);
        Assert.True(aggregate.TotalDistance > 0);
        Assert.Equal(1, aggregate.TerritoryCount);
        Assert.Equal(aggregate.TotalTerritoryScore, aggregate.StrongestTerritoryScore);
    }

    // ── Helper ───────────────────────────────────────────────────────

    private sealed class FixedScoreFormula : ITerritoryScoreFormula
    {
        private readonly double _fixedScore;
        public FixedScoreFormula(double fixedScore) => _fixedScore = fixedScore;
        public double ComputeBaseScore(RunInput run, double coverageRatio, TerritoryInput territory, ScoringFormulaConfig config) => _fixedScore;
    }
}
