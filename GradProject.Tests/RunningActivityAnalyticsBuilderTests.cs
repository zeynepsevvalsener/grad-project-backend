using GradProject.Domain.Entities;
using GradProject.Infrastructure.Services.Running;

namespace GradProject.Tests;

public class RunningActivityAnalyticsBuilderTests
{
    [Fact]
    public void Build_ComputesNegativeSplit_WhenSecondHalfFaster()
    {
        var activity = new RunningActivity
        {
            MovingTimeSeconds = 1200,
            ElapsedTimeSeconds = 1300,
            TotalElevationGain = 50,
            ElevHighMeters = 100,
            ElevLowMeters = 50,
            Splits =
            {
                new RunningActivitySplit { Ordinal = 0, DistanceMeters = 1000, MovingTimeSeconds = 300, ElapsedTimeSeconds = 310, ElevationDifferenceMeters = 0, AverageSpeedMetersPerSecond = 3.33, PaceSecondsPerKm = 300 },
                new RunningActivitySplit { Ordinal = 1, DistanceMeters = 1000, MovingTimeSeconds = 300, ElapsedTimeSeconds = 310, ElevationDifferenceMeters = 0, AverageSpeedMetersPerSecond = 3.33, PaceSecondsPerKm = 300 },
                new RunningActivitySplit { Ordinal = 2, DistanceMeters = 1000, MovingTimeSeconds = 280, ElapsedTimeSeconds = 290, ElevationDifferenceMeters = 0, AverageSpeedMetersPerSecond = 3.57, PaceSecondsPerKm = 280 },
                new RunningActivitySplit { Ordinal = 3, DistanceMeters = 1000, MovingTimeSeconds = 280, ElapsedTimeSeconds = 290, ElevationDifferenceMeters = 0, AverageSpeedMetersPerSecond = 3.57, PaceSecondsPerKm = 280 }
            }
        };

        var a = RunningActivityAnalyticsBuilder.Build(activity);

        Assert.NotNull(a);
        Assert.True(a.FirstHalfPaceSecondsPerKm > a.SecondHalfPaceSecondsPerKm);
        Assert.True(a.IsNegativeSplit);
        Assert.NotNull(a.MovingTimeRatio);
        Assert.InRange(a.MovingTimeRatio!.Value, 0.9, 1.0);
    }
}
