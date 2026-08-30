using AlzaLogistics.Core.Algorithms;
using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Tests;

public class GreedyKnapsackStrategyTests
{
    private readonly GreedyKnapsackStrategy _strategy = new();

    [Fact]
    public void SingleTrip_SinglePackage_Fits()
    {
        var request = new PlanningRequest
        {
            Packages = [new Package(0, 100.0, 1.0, 500m)],
            TripCount = 1,
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        Assert.Equal(1, result.AssignedPackageCount);
        Assert.Empty(result.UnassignedPackages);
        Assert.Equal(500m, result.TotalProfit);
    }

    [Fact]
    public void SingleTrip_SinglePackage_DoesNotFit()
    {
        var request = new PlanningRequest
        {
            Packages = [new Package(0, 6000.0, 1.0, 500m)],
            TripCount = 1,
            TripMaxWeightKg = 5500.0,
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        Assert.Equal(0, result.AssignedPackageCount);
        Assert.Single(result.UnassignedPackages);
    }

    [Fact]
    public void PrefersHighProfitDensity()
    {
        // Trip can hold either A or B, not both
        // A: heavy but high profit => lower density
        // B: light but moderate profit => higher density
        var a = new Package(0, 5000.0, 6.0, 1000m);
        var b = new Package(1, 100.0, 0.5, 800m);

        var request = new PlanningRequest
        {
            Packages = [a, b],
            TripCount = 1,
            TripMaxVolumeM3 = 6.0,
            TripMaxWeightKg = 5000.0,
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        // B has higher profit density, so it is selected over A
        Assert.Equal(1, result.AssignedPackageCount);
        Assert.Single(result.UnassignedPackages);
        Assert.Equal(a.Id, result.UnassignedPackages[0].Id);
        Assert.Equal(b.ProfitCzk, result.TotalProfit);
    }

    [Fact]
    public void MultipleTrips_DistributesPackages()
    {
        var packages = Enumerable
            .Range(0, 10)
            .Select(i => new Package(i, 3000.0, 4.0, 100m * (i + 1)))
            .ToArray();

        var request = new PlanningRequest
        {
            Packages = packages,
            TripCount = 5,
            TripMaxVolumeM3 = 7.0,
            TripMaxWeightKg = 5500.0,
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        // Each trip can hold 1 package (weight 3000 < 5500, volume 4 < 7, but 2nd doesn't fit)
        Assert.Equal(5, result.AssignedPackageCount);
        Assert.Equal(5, result.UnassignedPackages.Length);

        // Should prefer highest profit packages (IDs 9,8,7,6,5 with profits 1000,900,...,600)
        const decimal expectedProfit = 600m + 700m + 800m + 900m + 1000m;
        Assert.Equal(expectedProfit, result.TotalProfit);
    }

    [Fact]
    public void NoConstraintViolations_LargeScale()
    {
        var gen = new Core.DataGeneration.PackageGenerator(seed: 123);
        var packages = gen.Generate(5000);

        var request = new PlanningRequest
        {
            Packages = packages,
            TripCount = 20,
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        // Verify no trip exceeds capacity
        foreach (var trip in result.Trips)
        {
            Assert.True(trip.RemainingVolumeM3 >= -1e-9, $"Trip {trip.Id} volume exceeded: remaining={trip.RemainingVolumeM3}");
            Assert.True(trip.RemainingWeightKg >= -1e-9, $"Trip {trip.Id} weight exceeded: remaining={trip.RemainingWeightKg}");
        }

        // Verify totals are consistent
        Assert.Equal(5000, result.TotalPackages);
        Assert.Equal(result.TotalPackages, result.AssignedPackageCount + result.UnassignedPackages.Length + result.UnassignedMandatoryPackages.Length);
    }

    [Fact]
    public void EmptyInput_ReturnsEmptyResult()
    {
        var request = new PlanningRequest
        {
            Packages = [],
            TripCount = 5,
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        Assert.Equal(0, result.AssignedPackageCount);
        Assert.Empty(result.UnassignedPackages);
        Assert.Equal(0m, result.TotalProfit);
    }

    [Fact]
    public void AllPackagesFit()
    {
        var packages = Enumerable.Range(0, 5)
            .Select(i => new Package(i, 1.0, 0.1, 100m))
            .ToArray();

        var request = new PlanningRequest
        {
            Packages = packages,
            TripCount = 10,
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        Assert.Equal(5, result.AssignedPackageCount);
        Assert.Empty(result.UnassignedPackages);
        Assert.Equal(500m, result.TotalProfit);
    }

    [Fact]
    public void MandatoryPackages_PriorityOverDensity()
    {
        // Trip can hold exactly one package
        // Standard package has extremely high profit and fits
        // Mandatory package has low profit and fits
        var standardHigh = new Package(0, 100.0, 1.0, 10_000m, Priority.Standard);
        var mandatoryLow = new Package(1, 100.0, 1.0, 10m, Priority.Mandatory);

        var request = new PlanningRequest
        {
            Packages = [standardHigh, mandatoryLow],
            TripCount = 1,
            TripMaxVolumeM3 = 2.0,
            TripMaxWeightKg = 150.0,
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        // Only one can fit. Mandatory should win despite lower density.
        Assert.Equal(1, result.AssignedPackageCount);
        Assert.Single(result.UnassignedPackages);
        Assert.Empty(result.UnassignedMandatoryPackages);
        Assert.Equal(mandatoryLow.Id, result.Trips[0].Packages[0].Id);
        Assert.Equal(standardHigh.Id, result.UnassignedPackages[0].Id);
        Assert.Equal(10m, result.TotalProfit);
    }

    [Fact]
    public void MandatoryPackages_Overflow_ShouldBeCollected()
    {
        // 3 Mandatory packages, trip can only hold 1
        var a = new Package(0, 5000.0, 6.0, 100m, Priority.Mandatory);
        var b = new Package(1, 5000.0, 6.0, 200m, Priority.Mandatory);
        var c = new Package(2, 5000.0, 6.0, 300m, Priority.Mandatory);

        var request = new PlanningRequest
        {
            Packages = [a, b, c],
            TripCount = 1,
            TripMaxVolumeM3 = 6.0,
            TripMaxWeightKg = 5000.0,
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        Assert.Equal(1, result.AssignedPackageCount);
        Assert.Empty(result.UnassignedPackages);
        Assert.Equal(2, result.UnassignedMandatoryPackages.Length);
    }

    [Fact]
    public void Aging_OlderPackages_PriorityOverNewer()
    {
        // Two identical packages, but A has waited 5 days and B has waited 0 days.
        // A should have higher score and be picked over B.
        // C is a slightly higher profit package but 0 days, A should beat C with enough lambda.
        var a = new Package(0, 100.0, 1.0, 100m, Priority.Standard, 5);
        var c = new Package(1, 100.0, 1.0, 150m, Priority.Standard, 0);

        var request = new PlanningRequest
        {
            Packages = [a, c],
            TripCount = 1,
            TripMaxVolumeM3 = 1.0,
            TripMaxWeightKg = 100.0,
            AgingBoostLambda = 0.2, // A gets +100% boost -> effective profit 200 > 150
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        // A should win
        Assert.Equal(1, result.AssignedPackageCount);
        Assert.Equal(a.Id, result.Trips[0].Packages[0].Id);
        Assert.Equal(c.Id, result.UnassignedPackages[0].Id);
    }

    [Fact]
    public void ZeroTripCount_AllPackagesGoToUnassigned()
    {
        var standard = new Package(0, 10.0, 0.1, 100m, Priority.Standard);
        var mandatory = new Package(1, 10.0, 0.1, 100m, Priority.Mandatory);

        var request = new PlanningRequest
        {
            Packages = [standard, mandatory],
            TripCount = 0,
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        Assert.Equal(0, result.AssignedPackageCount);
        Assert.Single(result.UnassignedPackages);
        Assert.Equal(standard.Id, result.UnassignedPackages[0].Id);
        Assert.Single(result.UnassignedMandatoryPackages);
        Assert.Equal(mandatory.Id, result.UnassignedMandatoryPackages[0].Id);
        Assert.Equal(0m, result.TotalProfit);
    }

    [Fact]
    public void OversizedMandatoryPackage_GoesToUnassignedMandatory()
    {
        // Mandatory package that is larger than the trip max capacity
        var oversized = new Package(0, 6000.0, 8.0, 500m, Priority.Mandatory);

        var request = new PlanningRequest
        {
            Packages = [oversized],
            TripCount = 1,
            TripMaxVolumeM3 = 7.0,
            TripMaxWeightKg = 5500.0,
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        Assert.Equal(0, result.AssignedPackageCount);
        Assert.Empty(result.UnassignedPackages);
        Assert.Single(result.UnassignedMandatoryPackages);
        Assert.Equal(oversized.Id, result.UnassignedMandatoryPackages[0].Id);
    }

    [Fact]
    public void NegativeDaysWaiting_TreatedAsZero()
    {
        // Package with negative DaysWaiting should not reduce its profit score
        var normal = new Package(0, 100.0, 1.0, 100m, Priority.Standard, 0);
        var negativeDays = new Package(1, 100.0, 1.0, 100m, Priority.Standard, -5);

        var request = new PlanningRequest
        {
            Packages = [negativeDays, normal],
            TripCount = 1,
            TripMaxVolumeM3 = 1.0,
            TripMaxWeightKg = 100.0,
            AgingBoostLambda = 0.2,
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        // Score should be identical, tie-breaker picks index 0 (negativeDays)
        Assert.Equal(1, result.AssignedPackageCount);
        Assert.Equal(negativeDays.Id, result.Trips[0].Packages[0].Id);
    }

    [Fact]
    public void ExactCapacityFit_ConsumesAllCapacityExactly()
    {
        var exact = new Package(0, 5500.0, 7.0, 1000m, Priority.Standard);

        var request = new PlanningRequest
        {
            Packages = [exact],
            TripCount = 1,
            TripMaxVolumeM3 = 7.0,
            TripMaxWeightKg = 5500.0,
            LocalSearchTimeBudgetMs = 0
        };

        var result = _strategy.Plan(request);

        Assert.Equal(1, result.AssignedPackageCount);
        Assert.Equal(0.0, result.Trips[0].RemainingVolumeM3);
        Assert.Equal(0.0, result.Trips[0].RemainingWeightKg);
        Assert.Equal(1.0, result.Trips[0].VolumeUtilization);
        Assert.Equal(1.0, result.Trips[0].WeightUtilization);
    }
}
