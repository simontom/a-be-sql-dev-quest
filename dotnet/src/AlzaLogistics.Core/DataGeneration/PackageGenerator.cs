using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Core.DataGeneration;

/// <summary>
/// Generates realistic synthetic package data for testing and benchmarking.
/// Uses configurable distributions for weight, volume, and profit.
/// </summary>
public sealed class PackageGenerator(int seed = 42)
{
    private readonly Random _random = new(seed);

    // Realistic defaults based on e-commerce package statistics
    public double MinWeightKg { get; init; } = 0.1;
    public double MaxWeightKg { get; init; } = 30.0;
    public double MinVolumeM3 { get; init; } = 0.001;
    public double MaxVolumeM3 { get; init; } = 0.5;
    public decimal MinProfitCzk { get; init; } = 20m;
    public decimal MaxProfitCzk { get; init; } = 5_000m;

    /// <summary>
    /// Generates an array of packages with realistic distributions.
    /// Weight and volume follow a log-normal-like distribution (most packages are small).
    /// Profit correlates loosely with size but includes variance.
    /// </summary>
    public Package[] Generate(int count)
    {
        var packages = new Package[count];

        for (var i = 0; i < count; i++)
        {
            // Log-normal-ish distribution: most packages are small
            var weightFactor = Math.Pow(_random.NextDouble(), 2.0);
            var volumeFactor = Math.Pow(_random.NextDouble(), 2.0);

            var weight = MinWeightKg + weightFactor * (MaxWeightKg - MinWeightKg);
            var volume = MinVolumeM3 + volumeFactor * (MaxVolumeM3 - MinVolumeM3);

            // Profit loosely correlated with size + random component
            var sizeFactor = (weightFactor + volumeFactor) / 2.0;
            var profitFactor = 0.6 * sizeFactor + 0.4 * _random.NextDouble();
            var profit = MinProfitCzk + (decimal)(profitFactor * (double)(MaxProfitCzk - MinProfitCzk));

            packages[i] = new Package(i, Math.Round(weight, 3), Math.Round(volume, 4), Math.Round(profit, 2));
        }

        return packages;
    }
}
