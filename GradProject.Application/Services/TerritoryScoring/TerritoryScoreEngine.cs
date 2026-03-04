namespace GradProject.Application.Services.TerritoryScoring;

/// <summary>
/// Pure, deterministic territory score computation engine. No DB, no I/O, no time/random.
/// Computes run contributions per territory and user-level aggregates for leaderboard/claim/achievement.
/// </summary>
public sealed class TerritoryScoreEngine
{
    /// <summary>Decimal places for score rounding (determinism).</summary>
    public const int ScoreDecimalPlaces = 4;

    private readonly ITerritoryScoreFormula _formula;
    private readonly IRepeatEffectCalculator _repeatCalculator;

    public TerritoryScoreEngine(
        ITerritoryScoreFormula formula,
        IRepeatEffectCalculator repeatCalculator)
    {
        _formula = formula ?? throw new ArgumentNullException(nameof(formula));
        _repeatCalculator = repeatCalculator ?? throw new ArgumentNullException(nameof(repeatCalculator));
    }

    /// <summary>
    /// Computes per-territory contributions for one run. Same input always yields same output.
    /// </summary>
    /// <param name="run">Run with covered cells (or per-territory counts from HLN-7).</param>
    /// <param name="territories">Territories to evaluate.</param>
    /// <param name="userHistoryMap">userId -> (territoryId -> history). Can be empty.</param>
    /// <param name="config">Formula weights and repeat decay.</param>
    /// <returns>One contribution per territory where the run has coverage; empty when no coverage.</returns>
    public IReadOnlyList<TerritoryContribution> ComputeRunContributions(
        RunInput run,
        IReadOnlyList<TerritoryInput> territories,
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>> userHistoryMap,
        ScoringFormulaConfig config)
    {
        if (run == null) throw new ArgumentNullException(nameof(run));
        if (territories == null) throw new ArgumentNullException(nameof(territories));
        userHistoryMap ??= new Dictionary<int, IReadOnlyDictionary<int, UserTerritoryHistoryInput>>();
        if (config == null) throw new ArgumentNullException(nameof(config));

        var contributions = new List<TerritoryContribution>();

        foreach (var territory in territories)
        {
            int uniqueInTerritory = GetUniqueCoveredCellsInTerritory(run, territory);
            if (uniqueInTerritory <= 0) continue;

            double coverageRatio = territory.TotalCellCount > 0
                ? (double)uniqueInTerritory / territory.TotalCellCount
                : 0.0;
            if (coverageRatio <= 0) continue;

            double distanceInTerritory = run.DistanceMeters * coverageRatio;

            double baseScore = _formula.ComputeBaseScore(run, coverageRatio, territory, config);

            var history = userHistoryMap.TryGetValue(run.UserId, out var byTerritory) && byTerritory != null
                && byTerritory.TryGetValue(territory.Id, out var h)
                ? h
                : null;
            int previousCount = history?.PreviousContributionCount ?? 0;
            double repeatMultiplier = _repeatCalculator.GetMultiplier(previousCount, config.RepeatDecayFactor);

            double finalScore = baseScore * repeatMultiplier;
            if (config.TerritoryWeightEnabled && territory.Weight.HasValue)
                finalScore *= territory.Weight.Value;

            finalScore = Math.Round(finalScore, ScoreDecimalPlaces);
            baseScore = Math.Round(baseScore, ScoreDecimalPlaces);
            repeatMultiplier = Math.Round(repeatMultiplier, ScoreDecimalPlaces);
            coverageRatio = Math.Round(coverageRatio, ScoreDecimalPlaces);
            distanceInTerritory = Math.Round(distanceInTerritory, ScoreDecimalPlaces);

            contributions.Add(new TerritoryContribution
            {
                TerritoryId = territory.Id,
                BaseScore = baseScore,
                RepeatMultiplier = repeatMultiplier,
                FinalScore = finalScore,
                CoverageRatio = coverageRatio,
                DistanceInTerritory = distanceInTerritory,
                AveragePace = run.AveragePace
            });
        }

        return contributions;
    }

    /// <summary>
    /// Aggregates contributions into one user-level aggregate (totalTerritoryScore, totalDistance, averagePace, etc.).
    /// </summary>
    public UserTerritoryAggregate AggregateUserTerritoryScore(IReadOnlyList<TerritoryContribution> contributions)
    {
        if (contributions == null || contributions.Count == 0)
            return new UserTerritoryAggregate
            {
                TotalTerritoryScore = 0,
                TotalDistance = 0,
                AveragePace = 0,
                TerritoryCount = 0,
                StrongestTerritoryScore = 0
            };

        double totalScore = 0;
        double totalDistance = 0;
        double paceWeightedSum = 0;
        var territoryIds = new HashSet<int>();

        foreach (var c in contributions)
        {
            totalScore += c.FinalScore;
            totalDistance += c.DistanceInTerritory;
            paceWeightedSum += c.DistanceInTerritory * c.AveragePace;
            territoryIds.Add(c.TerritoryId);
        }

        double averagePace = totalDistance > 0 ? paceWeightedSum / totalDistance : 0;
        double strongest = contributions.Max(x => x.FinalScore);

        return new UserTerritoryAggregate
        {
            TotalTerritoryScore = Math.Round(totalScore, ScoreDecimalPlaces),
            TotalDistance = Math.Round(totalDistance, ScoreDecimalPlaces),
            AveragePace = Math.Round(averagePace, ScoreDecimalPlaces),
            TerritoryCount = territoryIds.Count,
            StrongestTerritoryScore = Math.Round(strongest, ScoreDecimalPlaces)
        };
    }

    private static int GetUniqueCoveredCellsInTerritory(RunInput run, TerritoryInput territory)
    {
        if (run.CoveredCellCountByTerritoryId != null &&
            run.CoveredCellCountByTerritoryId.TryGetValue(territory.Id, out int count))
            return count;

        if (run.CoveredCellIds == null || run.CoveredCellIds.Count == 0 || territory.CellIds == null || territory.CellIds.Count == 0)
            return 0;

        int unique = 0;
        foreach (var cellId in run.CoveredCellIds)
        {
            if (territory.CellIds.Contains(cellId))
                unique++;
        }
        return unique;
    }
}
