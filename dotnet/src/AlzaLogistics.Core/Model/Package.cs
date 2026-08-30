using System.Runtime.CompilerServices;

namespace AlzaLogistics.Core.Model;

/// <summary>
/// Represents a package/parcel with its physical properties and profit value.
/// Designed as a readonly record struct for zero-heap-allocation, cache-friendly storage.
/// </summary>
public readonly record struct Package(
    int Id,
    double WeightKg,
    double VolumeM3,
    decimal ProfitCzk,
    Priority Priority = Priority.Standard,
    int DaysWaiting = 0) : IComparable<Package>
{
    /// <summary>
    /// Compare by profit descending (higher profit first) for sorting.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Package other) => other.ProfitCzk.CompareTo(ProfitCzk);
}
