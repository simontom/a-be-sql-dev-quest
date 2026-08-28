using AlzaLogistics.Core.DataGeneration;
using AlzaLogistics.Core.Model;
using AlzaLogistics.Core.Services;

namespace AlzaLogistics.Tests;

public class PlanningServiceTests
{
    private readonly PlanningService _service = new();

    [Fact]
    public void Plan_EmptyPackages_ReturnsEmptyResult()
    {
        var request = new PlanningRequest { Packages = [] };

        var result = _service.Plan(request);

        Assert.Equal(0, result.TotalPackages);
        Assert.Equal("None (empty input)", result.AlgorithmName);
    }

    [Fact]
    public void Plan_LowDemand_UsesRoundRobin()
    {
        var packages = Enumerable
            .Range(0, 10)
            .Select(i => new Package(i, 1.0, 0.1, 100m))
            .ToArray();

        var request = new PlanningRequest
        {
            Packages = packages,
            TripCount = 5,
            DayType = DayType.LowDemand
        };

        var result = _service.Plan(request);

        Assert.Contains("Round-Robin", result.AlgorithmName);
        Assert.Equal(10, result.AssignedPackageCount);
        Assert.Empty(result.UnassignedPackages);
    }

    [Fact]
    public void Plan_HighDemand_UsesGreedyKnapsack()
    {
        var gen = new PackageGenerator(seed: 99);
        var packages = gen.Generate(1000);

        var request = new PlanningRequest
        {
            Packages = packages,
            TripCount = 5,
            DayType = DayType.HighDemand,
            LocalSearchTimeBudgetMs = 50
        };

        var result = _service.Plan(request);

        Assert.Contains("Greedy", result.AlgorithmName);
        Assert.True(result.TotalProfit > 0);
        Assert.True(result.AssignedPackageCount > 0);
        Assert.True(result.AssignedPackageCount < result.TotalPackages); // overcapacity scenario
    }

    [Fact]
    public void Plan_NullRequest_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Plan(null!));
    }

    [Fact]
    public void Plan_NullPackages_Throws()
    {
        var request = new PlanningRequest { Packages = null! };

        Assert.Throws<ArgumentNullException>(() => _service.Plan(request));
    }

    [Fact]
    public void Plan_ConstraintIntegrity_LargeScale()
    {
        var gen = new PackageGenerator(seed: 42);
        var packages = gen.Generate(10_000);

        var request = new PlanningRequest
        {
            Packages = packages,
            TripCount = 50,
            DayType = DayType.HighDemand,
            LocalSearchTimeBudgetMs = 100
        };

        var result = _service.Plan(request);

        // Constraint integrity
        foreach (var trip in result.Trips)
        {
            Assert.True(trip.RemainingVolumeM3 >= -1e-9);
            Assert.True(trip.RemainingWeightKg >= -1e-9);
        }

        // Consistency
        Assert.Equal(10_000, result.TotalPackages);
        Assert.Equal(result.TotalPackages, result.AssignedPackageCount + result.UnassignedPackages.Length);
    }
}
