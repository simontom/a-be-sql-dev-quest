using AlzaLogistics.Core.Algorithms;
using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Tests;

public class LocalSearchOptimizerTests
{
    [Fact]
    public void Optimize_SwapsLowProfitForHighProfit()
    {
        // Setup: trip has a low-profit package, and there's a higher-profit
        // unassigned package of equal or smaller size
        var trip = new Trip(0, maxVolumeM3: 1.0, maxWeightKg: 100.0);
        var lowProfit = new Package(1, 10.0, 0.5, 50m);
        trip.TryAdd(in lowProfit);

        var highProfit = new Package(2, 8.0, 0.4, 200m);
        var unassigned = new List<Package> { highProfit };

        var trips = new[] { trip };
        LocalSearchOptimizer.Optimize(trips, unassigned, timeBudgetMs: 1000);

        // After optimization, the trip should contain the high-profit package
        Assert.Equal(200m, trips[0].TotalProfit);
        Assert.Contains(unassigned, p => p.Id == 1); // low-profit moved to unassigned
    }

    [Fact]
    public void Optimize_NoSwapWhenUnassignedLowerProfit()
    {
        var trip = new Trip(0, maxVolumeM3: 1.0, maxWeightKg: 100.0);
        var highProfit = new Package(1, 10.0, 0.5, 500m);
        trip.TryAdd(in highProfit);

        var lowProfit = new Package(2, 8.0, 0.4, 50m);
        var unassigned = new List<Package> { lowProfit };

        var trips = new[] { trip };
        LocalSearchOptimizer.Optimize(trips, unassigned, timeBudgetMs: 1000);

        // No swap should occur
        Assert.Equal(500m, trips[0].TotalProfit);
        Assert.Single(unassigned);
        Assert.Equal(2, unassigned[0].Id);
    }

    [Fact]
    public void Optimize_NoSwapWhenCandidateDoesNotFit()
    {
        var trip = new Trip(0, maxVolumeM3: 1.0, maxWeightKg: 100.0);
        var existing = new Package(1, 10.0, 0.5, 50m);
        trip.TryAdd(in existing);

        // Higher profit but too large (even after removing existing)
        var tooBig = new Package(2, 200.0, 1.5, 200m);
        var unassigned = new List<Package> { tooBig };

        var trips = new[] { trip };
        LocalSearchOptimizer.Optimize(trips, unassigned, timeBudgetMs: 1000);

        // No swap should occur
        Assert.Equal(50m, trips[0].TotalProfit);
    }

    [Fact]
    public void Optimize_EmptyUnassigned_DoesNothing()
    {
        var trip = new Trip(0);
        trip.TryAdd(new Package(1, 10.0, 0.5, 100m));

        var trips = new[] { trip };
        var unassigned = new List<Package>();

        LocalSearchOptimizer.Optimize(trips, unassigned, timeBudgetMs: 1000);

        Assert.Equal(100m, trips[0].TotalProfit);
    }
}
