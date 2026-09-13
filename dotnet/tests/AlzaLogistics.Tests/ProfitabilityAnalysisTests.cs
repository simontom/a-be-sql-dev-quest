using System.Diagnostics;
using AlzaLogistics.Core.Algorithms;
using AlzaLogistics.Core.DataGeneration;
using AlzaLogistics.Core.Model;
using Xunit.Abstractions;

namespace AlzaLogistics.Tests;

public class ProfitabilityAnalysisTests(ITestOutputHelper output)
{
    private record StrategyResult(
        string Name,
        decimal TotalProfit,
        int AssignedCount,
        double VolumeUtilPct,
        double WeightUtilPct,
        long ElapsedMs);

    [Fact]
    public void EvaluateAllStrategies_100K_and_200K()
    {
        RunEvaluationForCount(100_000);
        RunEvaluationForCount(200_000);
    }

    private void RunEvaluationForCount(int count)
    {
        var gen = new PackageGenerator(seed: 42);
        var packages = gen.Generate(count);
        const int tripCount = 240;
        const double maxVol = 7.0;
        const double maxWeight = 5500.0;
        const double totalVolCap = tripCount * maxVol;
        const double totalWeightCap = tripCount * maxWeight;

        output.WriteLine($"==========================================================================");
        output.WriteLine($"EVALUATION FOR {count:N0} PACKAGES (240 trips, 1,680 m3, 1,320,000 kg)");
        output.WriteLine($"==========================================================================");

        // 1. LP Relaxation Upper Bound (Fractional Knapsack Upper Bound on Volume & Weight)
        var lpBound = ComputeLpRelaxationBound(packages, totalVolCap, totalWeightCap);
        output.WriteLine($"[THEORETICAL UPPER BOUND] LP Relaxation: {lpBound:N2} CZK");

        // We will test several strategies:
        var results = new List<StrategyResult>
        {
            // A. Baseline 1: Pure Profit (sort by Profit descending)
            RunCustomGreedy("1. Baseline: Pure Profit (desc)", packages, tripCount, maxVol, maxWeight,
                (p, vMax, wMax) => (double)p.ProfitCzk),
            // B. Baseline 2: Profit / Weight
            RunCustomGreedy("2. Baseline: Profit / Weight", packages, tripCount, maxVol, maxWeight,
                (p, vMax, wMax) => (double)p.ProfitCzk / (p.WeightKg / wMax)),
            // C. Baseline 3: Profit / Volume
            RunCustomGreedy("3. Baseline: Profit / Volume", packages, tripCount, maxVol, maxWeight,
                (p, vMax, wMax) => (double)p.ProfitCzk / (p.VolumeM3 / vMax)),
            // D. Balanced Density: alpha=0.5, beta=0.5
            RunCustomGreedy("4. Balanced: Profit / (0.5V + 0.5W)", packages, tripCount, maxVol, maxWeight,
                (p, vMax, wMax) => (double)p.ProfitCzk / (0.5 * (p.VolumeM3 / vMax) + 0.5 * (p.WeightKg / wMax))),
            // E. Tuned Density: alpha=0.8, beta=0.2
            RunCustomGreedy("5. Tuned: Profit / (0.8V + 0.2W)", packages, tripCount, maxVol, maxWeight,
                (p, vMax, wMax) => (double)p.ProfitCzk / (0.8 * (p.VolumeM3 / vMax) + 0.2 * (p.WeightKg / wMax))),
            // F. Tuned Density: alpha=0.9, beta=0.1
            RunCustomGreedy("6. Tuned: Profit / (0.9V + 0.1W)", packages, tripCount, maxVol, maxWeight,
                (p, vMax, wMax) => (double)p.ProfitCzk / (0.9 * (p.VolumeM3 / vMax) + 0.1 * (p.WeightKg / wMax))),
            // G. Balanced + Local Search (100ms) on pure packages (isolating optimization gain)
            RunCustomGreedyWithLocalSearch("7. Balanced + Local Search (100ms)", packages, tripCount, maxVol, maxWeight, 100)
        };

        // H. Business SLA Mode: Balanced with Mandatory + Aging
        var defaultStrategy = new GreedyKnapsackStrategy();
        var swSla = Stopwatch.StartNew();
        var slaResult = defaultStrategy.Plan(new PlanningRequest
        {
            Packages = packages,
            TripCount = tripCount,
            TripMaxVolumeM3 = maxVol,
            TripMaxWeightKg = maxWeight,
            AgingBoostLambda = 0.2,
            LocalSearchTimeBudgetMs = 0
        });
        swSla.Stop();
        results.Add(new StrategyResult(
            "8. Balanced + SLA/Aging Boost",
            slaResult.TotalProfit,
            slaResult.AssignedPackageCount,
            slaResult.Trips.Average(t => t.VolumeUtilization) * 100,
            slaResult.Trips.Average(t => t.WeightUtilization) * 100,
            swSla.ElapsedMilliseconds));

        output.WriteLine("{0,-36} | {1,18} | {2,10} | {3,10} | {4,10} | {5,8} | {6,10}", "Strategy", "Profit (CZK)", "% of LP Bound", "Vol Util %", "Wt Util %", "Pkgs", "Time (ms)");
        output.WriteLine(new string('-', 112));

        foreach (var r in results)
        {
            var pctOfLp = (double)(r.TotalProfit / lpBound) * 100.0;
            output.WriteLine("{0,-36} | {1,18:N2} | {2,9:F2}% | {3,9:F1}% | {4,9:F1}% | {5,8:N0} | {6,8}ms", r.Name, r.TotalProfit, pctOfLp, r.VolumeUtilPct, r.WeightUtilPct, r.AssignedCount, r.ElapsedMs);
        }

        output.WriteLine("");
    }

    private static StrategyResult RunCustomGreedy(
        string name,
        Package[] packages,
        int tripCount,
        double maxVol,
        double maxWeight,
        Func<Package, double, double, double> scoreFunc)
    {
        var sw = Stopwatch.StartNew();
        var trips = new Trip[tripCount];
        for (var i = 0; i < tripCount; i++)
        {
            trips[i] = new Trip(i, maxVol, maxWeight);
        }

        var scored = new (double Score, int Index)[packages.Length];
        for (var i = 0; i < packages.Length; i++)
        {
            var s = scoreFunc(packages[i], maxVol, maxWeight);
            scored[i] = (s, i);
        }

        Array.Sort(scored, (a, b) => b.Score.CompareTo(a.Score));

        var assignedCount = 0;
        var totalProfit = 0m;

        for (var s = 0; s < scored.Length; s++)
        {
            var idx = scored[s].Index;
            ref readonly var pkg = ref packages[idx];

            for (var t = 0; t < tripCount; t++)
            {
                if (trips[t].TryAdd(in pkg))
                {
                    assignedCount++;
                    totalProfit += pkg.ProfitCzk;
                    break;
                }
            }
        }

        sw.Stop();

        return new StrategyResult(
            name,
            totalProfit,
            assignedCount,
            trips.Average(t => t.VolumeUtilization) * 100,
            trips.Average(t => t.WeightUtilization) * 100,
            sw.ElapsedMilliseconds);
    }

    private static StrategyResult RunCustomGreedyWithLocalSearch(
        string name,
        Package[] packages,
        int tripCount,
        double maxVol,
        double maxWeight,
        int timeBudgetMs)
    {
        var sw = Stopwatch.StartNew();
        var trips = new Trip[tripCount];
        for (var i = 0; i < tripCount; i++)
        {
            trips[i] = new Trip(i, maxVol, maxWeight);
        }

        var scored = new (double Score, int Index)[packages.Length];
        for (var i = 0; i < packages.Length; i++)
        {
            var s = (double)packages[i].ProfitCzk / (0.5 * (packages[i].VolumeM3 / maxVol) + 0.5 * (packages[i].WeightKg / maxWeight));
            scored[i] = (s, i);
        }

        Array.Sort(scored, (a, b) => b.Score.CompareTo(a.Score));

        var assigned = new bool[packages.Length];
        for (var s = 0; s < scored.Length; s++)
        {
            var idx = scored[s].Index;
            ref readonly var pkg = ref packages[idx];

            for (var t = 0; t < tripCount; t++)
            {
                if (trips[t].TryAdd(in pkg))
                {
                    assigned[idx] = true;
                    break;
                }
            }
        }

        var unassigned = new List<Package>();
        for (var i = 0; i < packages.Length; i++)
        {
            if (!assigned[i]) unassigned.Add(packages[i]);
        }

        LocalSearchOptimizer.Optimize(trips, unassigned, timeBudgetMs);

        sw.Stop();

        return new StrategyResult(
            name,
            trips.Sum(t => t.TotalProfit),
            trips.Sum(t => t.PackageCount),
            trips.Average(t => t.VolumeUtilization) * 100,
            trips.Average(t => t.WeightUtilization) * 100,
            sw.ElapsedMilliseconds);
    }

    private static decimal ComputeLpRelaxationBound(Package[] packages, double totalVolCap, double totalWeightCap)
    {
        // 1D fractional knapsack on volume (since volume is the strictly binding constraint)
        var sortedByVolDensity = packages
            .Select(p => new { Package = p, Density = (double)p.ProfitCzk / p.VolumeM3 })
            .OrderByDescending(x => x.Density)
            .ToArray();

        var remainingVol = totalVolCap;
        var totalProfitVol = 0m;

        foreach (var item in sortedByVolDensity)
        {
            if (remainingVol <= 0) break;
            if (item.Package.VolumeM3 <= remainingVol)
            {
                totalProfitVol += item.Package.ProfitCzk;
                remainingVol -= item.Package.VolumeM3;
            }
            else
            {
                var fraction = (decimal)(remainingVol / item.Package.VolumeM3);
                totalProfitVol += item.Package.ProfitCzk * fraction;
                remainingVol = 0;
                break;
            }
        }

        // Also check weight constraint
        var sortedByWeightDensity = packages
            .Select(p => new { Package = p, Density = (double)p.ProfitCzk / p.WeightKg })
            .OrderByDescending(x => x.Density)
            .ToArray();

        var remainingWeight = totalWeightCap;
        var totalProfitWeight = 0m;

        foreach (var item in sortedByWeightDensity)
        {
            if (remainingWeight <= 0) break;
            if (item.Package.WeightKg <= remainingWeight)
            {
                totalProfitWeight += item.Package.ProfitCzk;
                remainingWeight -= item.Package.WeightKg;
            }
            else
            {
                var fraction = (decimal)(remainingWeight / item.Package.WeightKg);
                totalProfitWeight += item.Package.ProfitCzk * fraction;
                remainingWeight = 0;
                break;
            }
        }

        return Math.Min(totalProfitVol, totalProfitWeight);
    }
}
