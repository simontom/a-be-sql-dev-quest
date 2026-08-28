using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Tests;

public class TripTests
{
    [Fact]
    public void Trip_DefaultCapacity()
    {
        var trip = new Trip(0);

        Assert.Equal(Trip.DefaultMaxVolumeM3, trip.MaxVolumeM3);
        Assert.Equal(Trip.DefaultMaxWeightKg, trip.MaxWeightKg);
        Assert.Equal(Trip.DefaultMaxVolumeM3, trip.RemainingVolumeM3);
        Assert.Equal(Trip.DefaultMaxWeightKg, trip.RemainingWeightKg);
    }

    [Fact]
    public void TryAdd_PackageFits_ReturnsTrue()
    {
        var trip = new Trip(0, maxVolumeM3: 10.0, maxWeightKg: 100.0);

        var pkg = new Package(1, 5.0, 3.0, 100m);

        Assert.True(trip.TryAdd(in pkg));
        Assert.Equal(1, trip.PackageCount);
        Assert.Equal(7.0, trip.RemainingVolumeM3, precision: 6);
        Assert.Equal(95.0, trip.RemainingWeightKg, precision: 6);
        Assert.Equal(100m, trip.TotalProfit);
    }

    [Fact]
    public void TryAdd_ExceedsVolume_ReturnsFalse()
    {
        var trip = new Trip(0, maxVolumeM3: 1.0, maxWeightKg: 100.0);

        var pkg = new Package(1, 5.0, 1.1, 100m);

        Assert.False(trip.TryAdd(in pkg));
        Assert.Equal(0, trip.PackageCount);
    }

    [Fact]
    public void TryAdd_ExceedsWeight_ReturnsFalse()
    {
        var trip = new Trip(0, maxVolumeM3: 10.0, maxWeightKg: 10.0);

        var pkg = new Package(1, 11.0, 1.0, 100m);

        Assert.False(trip.TryAdd(in pkg));
        Assert.Equal(0, trip.PackageCount);
    }

    [Fact]
    public void TryAdd_ExactlyFits_ReturnsTrue()
    {
        var trip = new Trip(0, maxVolumeM3: 1.0, maxWeightKg: 10.0);

        var pkg = new Package(1, 10.0, 1.0, 50m);

        Assert.True(trip.TryAdd(in pkg));
        Assert.Equal(0.0, trip.RemainingVolumeM3, precision: 6);
        Assert.Equal(0.0, trip.RemainingWeightKg, precision: 6);
    }

    [Fact]
    public void TryAdd_MultiplePackages_TracksCumulative()
    {
        var trip = new Trip(0, maxVolumeM3: 5.0, maxWeightKg: 100.0);

        Assert.True(trip.TryAdd(new Package(1, 10.0, 2.0, 100m)));
        Assert.True(trip.TryAdd(new Package(2, 20.0, 2.0, 200m)));
        Assert.False(trip.TryAdd(new Package(3, 5.0, 2.0, 300m))); // volume exceeded
        Assert.Equal(2, trip.PackageCount);
        Assert.Equal(300m, trip.TotalProfit);
    }

    [Fact]
    public void Utilization_ReflectsUsage()
    {
        var trip = new Trip(0, maxVolumeM3: 10.0, maxWeightKg: 100.0);

        trip.TryAdd(new Package(1, 50.0, 5.0, 100m));

        Assert.Equal(0.5, trip.VolumeUtilization, precision: 6);
        Assert.Equal(0.5, trip.WeightUtilization, precision: 6);
    }
}
