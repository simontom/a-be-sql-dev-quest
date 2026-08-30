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

    [Fact]
    public void Optimize_WithMandatoryAndStandard_SwapsStandardOnly()
    {
        // Trip contains:
        // 1. Mandatory package with profit 10 (low profit)
        // 2. Standard package with profit 50
        // Candidate: Standard package with profit 300
        // Expected: Mandatory is preserved, Standard is replaced by Candidate
        var trip = new Trip(0, maxVolumeM3: 2.0, maxWeightKg: 200.0);
        var mandatory = new Package(1, 10.0, 0.5, 10m, Priority.Mandatory);
        var standardLow = new Package(2, 10.0, 0.5, 50m, Priority.Standard);
        trip.TryAdd(in mandatory);
        trip.TryAdd(in standardLow);

        var candidate = new Package(3, 10.0, 0.5, 300m, Priority.Standard);
        var unassigned = new List<Package> { candidate };

        var trips = new[] { trip };
        LocalSearchOptimizer.Optimize(trips, unassigned, timeBudgetMs: 1000);

        // Profit should be 10 (mandatory) + 300 (candidate) = 310
        Assert.Equal(310m, trip.TotalProfit);
        Assert.Contains(trip.Packages, p => p.Id == 1 && p.Priority == Priority.Mandatory);
        Assert.Contains(trip.Packages, p => p.Id == 3);
        Assert.Contains(unassigned, p => p.Id == 2);
    }

    [Fact]
    public void Optimize_TripContainsOnlyMandatory_NoSwapOccurs()
    {
        // Trip has only mandatory packages, none can be evicted
        var trip = new Trip(0, maxVolumeM3: 1.0, maxWeightKg: 100.0);
        var mandatory = new Package(1, 10.0, 0.5, 10m, Priority.Mandatory);
        trip.TryAdd(in mandatory);

        var candidate = new Package(2, 10.0, 0.5, 500m, Priority.Standard);
        var unassigned = new List<Package> { candidate };

        var trips = new[] { trip };
        LocalSearchOptimizer.Optimize(trips, unassigned, timeBudgetMs: 1000);

        Assert.Equal(10m, trip.TotalProfit);
        Assert.Single(unassigned);
        Assert.Equal(2, unassigned[0].Id);
    }
}
