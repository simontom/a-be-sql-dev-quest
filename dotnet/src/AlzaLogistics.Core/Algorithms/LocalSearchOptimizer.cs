using System.Diagnostics;
using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Core.Algorithms;

/// <summary>
/// Time-bounded local search optimizer that tries to improve the greedy solution
/// by swapping low-value assigned packages with higher-value unassigned packages.
/// 
/// Strategy: For each unassigned package (highest profit first), try to find a 
/// trip where removing the lowest-profit assigned package would make room for
/// the unassigned package, yielding a net profit improvement.
/// </summary>
public static class LocalSearchOptimizer
{
    public static void Optimize(Trip[] trips, List<Package> unassigned, int timeBudgetMs)
    {
        if (unassigned.Count == 0)
        {
            return;
        }

        var sw = Stopwatch.StartNew();

        // Sort unassigned by profit descending to prioritize high-value swaps
        unassigned.Sort((a, b) => b.ProfitCzk.CompareTo(a.ProfitCzk));

        var improved = true;
        while (improved && sw.ElapsedMilliseconds < timeBudgetMs)
        {
            improved = false;

            for (var u = unassigned.Count - 1; u >= 0; u--)
            {
                if (sw.ElapsedMilliseconds >= timeBudgetMs)
                {
                    break;
                }

                var candidate = unassigned[u];
                var swapped = false;

                for (var t = 0; t < trips.Length && !swapped; t++)
                {
                    var trip = trips[t];
                    if (trip.PackageCount == 0)
                    {
                        continue;
                    }

                    // Find the lowest-profit package in this trip
                    var tripPackages = trip.Packages;
                    var worstIdx = 0;
                    var worstProfit = tripPackages[0].ProfitCzk;

                    for (var p = 1; p < tripPackages.Count; p++)
                    {
                        if (tripPackages[p].Priority == Priority.Mandatory) continue;

                        if (tripPackages[p].ProfitCzk < worstProfit)
                        {
                            worstProfit = tripPackages[p].ProfitCzk;
                            worstIdx = p;
                        }
                    }

                    var worstPkg = tripPackages[worstIdx];
                    if (worstPkg.Priority == Priority.Mandatory)
                    {
                        continue; // Cannot swap out mandatory packages (edge case if first package is mandatory)
                    }

                    // Check if swapping yields net improvement and fits
                    if (candidate.ProfitCzk <= worstProfit)
                    {
                        continue;
                    }

                    var newRemainingVol = trip.RemainingVolumeM3 + worstPkg.VolumeM3 - candidate.VolumeM3;
                    var newRemainingWt = trip.RemainingWeightKg + worstPkg.WeightKg - candidate.WeightKg;

                    if (newRemainingVol >= 0 && newRemainingWt >= 0)
                    {
                        // Perform swap in-place
                        trip.RemoveAt(worstIdx);
                        trip.TryAdd(in candidate);

                        // Move worst package to unassigned, remove candidate from unassigned
                        unassigned[u] = worstPkg;
                        improved = true;
                        swapped = true;
                    }
                }
            }
        }
    }
}
