# Walkthrough — Quest 2: Doručení teleportem

## Summary

Built a complete .NET 10 solution implementing an optimal delivery capacity planner that allocates 100K+ packages across 240 delivery trips, maximizing profit under volume (7 m³) and weight (5,500 kg) constraints.

## Changes Made

### Solution Structure Created
- **`AlzaLogistics.slnx`** — Solution file with 4 projects
- **`src/AlzaLogistics.Core/`** — Class library (domain model + algorithms)
- **`src/AlzaLogistics.Console/`** — CLI demo application
- **`tests/AlzaLogistics.Tests/`** — xUnit test suite (34 tests)
- **`tests/AlzaLogistics.Benchmarks/`** — BenchmarkDotNet performance suite
- **`docs/solution-description.md`** — Required written explanation

### Core Domain Model (5 files)
- `Package.cs` — `readonly record struct` with Id, WeightKg, VolumeM3, ProfitCzk
- `Trip.cs` — Capacity-constrained container with TryAdd, utilization tracking
- `DayType.cs` — LowDemand/HighDemand enum
- `PlanningRequest.cs` — Input DTO
- `PackageGenerator.cs` — Synthetic data generator (log-normal distributions)

### Algorithms (4 files)
- `IPlanningStrategy.cs` — Strategy interface
- `PackageScorer.cs` — Profit-density scoring
- `GreedyKnapsackStrategy.cs` — Greedy knapsack algorithm
- `LocalSearchOptimizer.cs` — Refinement pass

### Service & Console
- `PlanningService.cs` — Planner orchestrator
- `Program.cs` — Console entry point

### Documentation (docs/)
- `solution-description.md` — Explains design & choice of algorithms
- `complexity_analysis.md` — Asymptotic time & space complexities
- `benchmark_results.md` — Concrete execution times & allocations

## Verification Results

### Build
- ✅ Release build: **0 warnings, 0 errors**

### Tests
- ✅ **34/34 tests pass** (1.3 seconds total)

### Benchmarks (100K packages)
- **Round-Robin**: 93 ms
- **Greedy Only**: 80 ms
- **Greedy + Local Search**: 178 ms
