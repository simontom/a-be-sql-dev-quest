using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Tests;

public class PackageTests
{
    [Fact]
    public void Package_IsValueType()
    {
        var pkg = new Package(1, 2.5, 0.1, 100m);

        Assert.Equal(1, pkg.Id);
        Assert.Equal(2.5, pkg.WeightKg);
        Assert.Equal(0.1, pkg.VolumeM3);
        Assert.Equal(100m, pkg.ProfitCzk);
    }

    [Fact]
    public void Package_EqualityByValue()
    {
        var a = new Package(1, 2.5, 0.1, 100m);
        var b = new Package(1, 2.5, 0.1, 100m);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Package_InequalityByDifferentValues()
    {
        var a = new Package(1, 2.5, 0.1, 100m);
        var b = new Package(2, 2.5, 0.1, 200m);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Package_CompareTo_HigherProfitFirst()
    {
        var low = new Package(1, 1.0, 0.1, 50m);
        var high = new Package(2, 1.0, 0.1, 200m);

        Assert.True(high.CompareTo(low) < 0); // high sorts before low
        Assert.True(low.CompareTo(high) > 0);
    }

    [Fact]
    public void Package_SortDescendingByProfit()
    {
        var packages = new[]
        {
            new Package(1, 1.0, 0.1, 50m),
            new Package(2, 1.0, 0.1, 300m),
            new Package(3, 1.0, 0.1, 100m)
        };

        Array.Sort(packages);

        Assert.Equal(300m, packages[0].ProfitCzk);
        Assert.Equal(100m, packages[1].ProfitCzk);
        Assert.Equal(50m, packages[2].ProfitCzk);
    }
}
