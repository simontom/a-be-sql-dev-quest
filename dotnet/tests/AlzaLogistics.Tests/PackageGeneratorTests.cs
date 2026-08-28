using AlzaLogistics.Core.DataGeneration;

namespace AlzaLogistics.Tests;

public class PackageGeneratorTests
{
    [Fact]
    public void Generate_ReturnsCorrectCount()
    {
        var gen = new PackageGenerator();

        var packages = gen.Generate(100);

        Assert.Equal(100, packages.Length);
    }

    [Fact]
    public void Generate_IdsAreSequential()
    {
        var gen = new PackageGenerator();

        var packages = gen.Generate(50);

        for (var i = 0; i < packages.Length; i++)
        {
            Assert.Equal(i, packages[i].Id);
        }
    }

    [Fact]
    public void Generate_ValuesWithinBounds()
    {
        var gen = new PackageGenerator();

        var packages = gen.Generate(10_000);

        foreach (var p in packages)
        {
            Assert.InRange(p.WeightKg, 0.1, 30.0);
            Assert.InRange(p.VolumeM3, 0.001, 0.5);
            Assert.True(p.ProfitCzk >= 20m);
            Assert.True(p.ProfitCzk <= 5000m);
        }
    }

    [Fact]
    public void Generate_DeterministicWithSameSeed()
    {
        var gen1 = new PackageGenerator(seed: 123);
        var gen2 = new PackageGenerator(seed: 123);

        var a = gen1.Generate(100);
        var b = gen2.Generate(100);

        for (var i = 0; i < a.Length; i++)
        {
            Assert.Equal(a[i], b[i]);
        }
    }

    [Fact]
    public void Generate_DifferentSeedsDifferentData()
    {
        var gen1 = new PackageGenerator(seed: 1);
        var gen2 = new PackageGenerator(seed: 2);

        var a = gen1.Generate(100);
        var b = gen2.Generate(100);

        // At least some should differ
        Assert.Contains(a.Zip(b), pair => pair.First != pair.Second);
    }
}
