using System.Buffers;
using System.Diagnostics;
using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Core.Algorithms;

/// <summary>
/// Greedy heuristic for multi-dimensional multi-container knapsack.
/// 
/// Algorithm:
/// 1. Score each package by profit-density (profit / weighted resource consumption)
/// 2. Sort packages descending by density score
/// 3. For each package, try to assign to the first trip with sufficient capacity
/// 4. Optionally apply local search refinement within time budget
/// 
/// Time complexity: O(n log n) for sorting + O(n * m) for assignment
/// where n = packages, m = trips
/// </summary>
public sealed class GreedyKnapsackStrategy : IPlanningStrategy
{
    public string Name => "Greedy Knapsack (Profit-Density)";

    public PlanningResult Plan(PlanningRequest request)
    {
        var sw = Stopwatch.StartNew();
        var packages = request.Packages;
        var tripCount = request.TripCount;

        // Initialize trips
        var trips = new Trip[tripCount];
        for (var i = 0; i < tripCount; i++)
        {
            trips[i] = new Trip(i, request.TripMaxVolumeM3, request.TripMaxWeightKg);
        }

        var scoredBuffer = ArrayPool<PackageScorer.ScoredPackage>.Shared.Rent(packages.Length);
        var assignedBuffer = ArrayPool<bool>.Shared.Rent(packages.Length);

        try
        {
            var scored = scoredBuffer.AsSpan(0, packages.Length);
            var assigned = assignedBuffer.AsSpan(0, packages.Length);
            assigned.Clear();
            var unassignedMandatory = new List<Package>();

            // Phase 1: Assign mandatory packages
            for (var i = 0; i < packages.Length; i++)
            {
                if (packages[i].Priority == Priority.Mandatory)
                {
                    ref readonly var pkg = ref packages[i];
                    var placed = false;
                    for (var t = 0; t < tripCount; t++)
                    {
                        if (trips[t].TryAdd(in pkg))
                        {
                            assigned[i] = true;
                            placed = true;
                            break;
                        }
                    }
                    if (!placed)
                    {
                        unassignedMandatory.Add(pkg);
                    }
                }
            }

            // Phase 2: Score and sort packages by profit-density (with aging)
            PackageScorer.ScoreAndSort(
                packages.AsSpan(),
                scored,
                request.TripMaxVolumeM3,
                request.TripMaxWeightKg,
                0.5, 0.5, request.AgingBoostLambda);

            // Greedy first-fit-decreasing assignment for remaining packages
            for (var s = 0; s < scored.Length; s++)
            {
                var pkgIdx = scored[s].OriginalIndex;
                if (assigned[pkgIdx])
                {
                    continue; // Skip already assigned mandatory packages
                }

                ref readonly var pkg = ref packages[pkgIdx];

                for (var t = 0; t < tripCount; t++)
                {
                    if (trips[t].TryAdd(in pkg))
                    {
                        assigned[pkgIdx] = true;
                        break;
                    }
                }
            }

            // Collect unassigned standard/elevated packages
            var unassigned = new List<Package>();
            for (var i = 0; i < packages.Length; i++)
            {
                if (!assigned[i] && packages[i].Priority != Priority.Mandatory)
                {
                    unassigned.Add(packages[i]);
                }
            }

            // Apply local search if time budget allows
            if (request.LocalSearchTimeBudgetMs > 0 && unassigned.Count > 0)
            {
                LocalSearchOptimizer.Optimize(trips, unassigned, request.LocalSearchTimeBudgetMs);
            }

            sw.Stop();

            return new PlanningResult
            {
                Trips = trips,
                UnassignedPackages = unassigned.ToArray(),
                UnassignedMandatoryPackages = unassignedMandatory.ToArray(),
                TotalPackages = packages.Length,
                ElapsedTime = sw.Elapsed,
                AlgorithmName = Name
            };
        }
        finally
        {
            ArrayPool<PackageScorer.ScoredPackage>.Shared.Return(scoredBuffer);
            ArrayPool<bool>.Shared.Return(assignedBuffer);
        }
    }
}
