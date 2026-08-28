using System.Buffers;
using System.Runtime.CompilerServices;
using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Core.Algorithms;

/// <summary>
/// Computes profit-density scores for packages using a weighted combination
/// of normalized volume and weight consumption.
/// </summary>
public static class PackageScorer
{
    /// <summary>
    /// Scoring tuple stored as a value type for zero-alloc sorting.
    /// </summary>
    public readonly record struct ScoredPackage(
        double Score,
        int OriginalIndex) : IComparable<ScoredPackage>
    {
        /// <summary>
        /// Descending sort by score (highest density first).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(ScoredPackage other) => other.Score.CompareTo(Score);
    }

    /// <summary>
    /// Computes density scores for all packages.
    /// Score = Profit / (alpha * NormalizedVolume + beta * NormalizedWeight)
    /// where normalization is relative to trip capacity.
    /// </summary>
    /// <param name="packages">Source packages array.</param>
    /// <param name="tripMaxVolume">Max volume per trip (for normalization).</param>
    /// <param name="tripMaxWeight">Max weight per trip (for normalization).</param>
    /// <param name="alpha">Weight factor for volume dimension (default 0.5).</param>
    /// <param name="beta">Weight factor for weight dimension (default 0.5).</param>
    /// <returns>Array of scored packages sorted descending by density score.</returns>
    public static void ScoreAndSort(
        ReadOnlySpan<Package> packages,
        Span<ScoredPackage> scoredBuffer,
        double tripMaxVolume,
        double tripMaxWeight,
        double alpha = 0.5,
        double beta = 0.5)
    {
        for (var i = 0; i < packages.Length; i++)
        {
            ref readonly var p = ref packages[i];
            var normalizedVolume = p.VolumeM3 / tripMaxVolume;
            var normalizedWeight = p.WeightKg / tripMaxWeight;
            var cost = alpha * normalizedVolume + beta * normalizedWeight;

            // Guard against zero-size packages (assign max score)
            var score = cost > 0
                ? (double)p.ProfitCzk / cost
                : double.MaxValue;

            scoredBuffer[i] = new ScoredPackage(score, i);
        }

        scoredBuffer.Sort();
    }
}
