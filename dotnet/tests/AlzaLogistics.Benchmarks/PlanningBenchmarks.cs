using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using AlzaLogistics.Core.Algorithms;
using AlzaLogistics.Core.DataGeneration;
using AlzaLogistics.Core.Model;
using AlzaLogistics.Core.Services;

namespace AlzaLogistics.Benchmarks;

/// <summary>
/// Performance benchmarks for the delivery planning algorithm.
/// Target: less than 1 second for 100K packages across 240 trips.
/// </summary>
[MemoryDiagnoser]
public class PlanningBenchmarks
{
    private Package[] _packages100K = null!;
    private Package[] _packages250K = null!;
    private Package[] _packages500K = null!;
    private PlanningService _service = null!;

    [GlobalSetup]
    public void Setup()
    {
        var gen = new PackageGenerator(seed: 42);
        _packages100K = gen.Generate(100_000);
        _packages250K = gen.Generate(250_000);
        _packages500K = gen.Generate(500_000);
        _service = new PlanningService();
    }

    [Benchmark(Description = "100K packages, 240 trips, greedy only")]
    public PlanningResult Plan_100K_GreedyOnly()
    {
        return _service.Plan(new PlanningRequest
        {
            Packages = _packages100K,
            TripCount = 240,
            DayType = DayType.HighDemand,
            LocalSearchTimeBudgetMs = 0
        });
    }

    [Benchmark(Description = "100K packages, 240 trips, greedy + local search")]
    public PlanningResult Plan_100K_WithLocalSearch()
    {
        return _service.Plan(new PlanningRequest
        {
            Packages = _packages100K,
            TripCount = 240,
            DayType = DayType.HighDemand,
            LocalSearchTimeBudgetMs = 100
        });
    }

    [Benchmark(Description = "250K packages, 240 trips, greedy only")]
    public PlanningResult Plan_250K_GreedyOnly()
    {
        return _service.Plan(new PlanningRequest
        {
            Packages = _packages250K,
            TripCount = 240,
            DayType = DayType.HighDemand,
            LocalSearchTimeBudgetMs = 0
        });
    }

    [Benchmark(Description = "500K packages, 240 trips, greedy only")]
    public PlanningResult Plan_500K_GreedyOnly()
    {
        return _service.Plan(new PlanningRequest
        {
            Packages = _packages500K,
            TripCount = 240,
            DayType = DayType.HighDemand,
            LocalSearchTimeBudgetMs = 0
        });
    }

    // Low Demand (Tue/Thu): round-robin — all packages fit, no optimization needed
    [Benchmark(Description = "100K packages, 240 trips, round-robin (Low Demand)")]
    public PlanningResult Plan_100K_LowDemand()
    {
        return _service.Plan(new PlanningRequest
        {
            Packages = _packages100K,
            TripCount = 240,
            DayType = DayType.LowDemand,
            LocalSearchTimeBudgetMs = 0
        });
    }
}
