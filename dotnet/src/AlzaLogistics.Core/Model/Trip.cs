namespace AlzaLogistics.Core.Model;

/// <summary>
/// Represents a single delivery trip with volume and weight capacity constraints.
/// Tracks remaining capacity and assigned packages.
/// </summary>
public sealed class Trip(
    int id,
    double maxVolumeM3 = Trip.DefaultMaxVolumeM3,
    double maxWeightKg = Trip.DefaultMaxWeightKg
)
{
    public const double DefaultMaxVolumeM3 = 7.0;
    public const double DefaultMaxWeightKg = 5_500.0;

    private readonly List<Package> _packages = new();

    public int Id { get; } = id;
    public double MaxVolumeM3 { get; } = maxVolumeM3;
    public double MaxWeightKg { get; } = maxWeightKg;
    public double RemainingVolumeM3 { get; private set; } = maxVolumeM3;
    public double RemainingWeightKg { get; private set; } = maxWeightKg;
    public decimal TotalProfit { get; private set; }
    public IReadOnlyList<Package> Packages => _packages;
    public int PackageCount => _packages.Count;

    /// <summary>
    /// Attempts to add a package to this trip. Returns true if the package fits.
    /// </summary>
    public bool TryAdd(in Package package)
    {
        if (package.VolumeM3 > RemainingVolumeM3 || package.WeightKg > RemainingWeightKg)
        {
            return false;
        }

        _packages.Add(package);
        RemainingVolumeM3 -= package.VolumeM3;
        RemainingWeightKg -= package.WeightKg;
        TotalProfit += package.ProfitCzk;
        return true;
    }

    /// <summary>
    /// Returns the volume utilization as a fraction [0..1].
    /// </summary>
    public double VolumeUtilization => 1.0 - (RemainingVolumeM3 / MaxVolumeM3);

    /// <summary>
    /// Returns the weight utilization as a fraction [0..1].
    /// </summary>
    public double WeightUtilization => 1.0 - (RemainingWeightKg / MaxWeightKg);

    /// <summary>
    /// Removes a package from the trip at the specified index, restoring capacity.
    /// </summary>
    public void RemoveAt(int index)
    {
        var package = _packages[index];
        _packages.RemoveAt(index);
        RemainingVolumeM3 += package.VolumeM3;
        RemainingWeightKg += package.WeightKg;
        TotalProfit -= package.ProfitCzk;
    }
}
