# Optimal Delivery Capacity Planning — Implementation Plan

## Problem Summary

Design and implement a C# (.NET 8+) module that allocates **100,000+ packages** across **240 delivery trips** (120 vans × 2 trips/day), each constrained by **7 m³ volume** and **5,500 kg weight**, to **maximize total profit** — all within sub-second computation time.

This is a **Multi-dimensional Multi-container Knapsack Problem** (NP-hard). We use a high-performance greedy heuristic with optional local search refinement.

---

## Solution Architecture

```
AlzaLogistics/
├── AlzaLogistics.sln
├── src/
│   ├── AlzaLogistics.Core/          # Domain model + algorithms (class library)
│   └── AlzaLogistics.Console/       # CLI demo + result visualization
├── tests/
│   ├── AlzaLogistics.Tests/         # Unit + integration tests (xUnit)
│   └── AlzaLogistics.Benchmarks/    # BenchmarkDotNet performance tests
└── docs/
    └── solution-description.md      # Required written explanation
```

---

## Proposed Changes

### Core Domain Model (`AlzaLogistics.Core/Model/`)

#### [NEW] `Package.cs`
- `readonly record struct Package(int Id, double WeightKg, double VolumeM3, decimal ProfitCzk)`
- Compact value type — zero heap allocation per instance
- Implements `IComparable<Package>` for efficient sorting

#### [NEW] `Trip.cs`
- `class Trip` with `MaxVolumeM3 = 7.0`, `MaxWeightKg = 5500.0`
- Tracks `RemainingVolume`, `RemainingWeight`, assigned package list
- `bool TryAdd(in Package p)` — atomic fit-check + add

#### [NEW] `PlanningRequest.cs`
- Input DTO: list of packages, number of trips, day type, optional algorithm config

#### [NEW] `PlanningResult.cs`
- Output DTO: per-trip assignments, total profit, unassigned packages, timing stats

#### [NEW] `DayType.cs`
- `enum DayType { LowDemand, HighDemand }` — Tue/Thu vs. other days

---

### Algorithm Layer (`AlzaLogistics.Core/Algorithms/`)

#### [NEW] `IPlanningStrategy.cs`
- Strategy interface: `PlanningResult Plan(PlanningRequest request)`
- Enables swapping algorithms without changing callers

#### [NEW] `GreedyKnapsackStrategy.cs`
**Primary algorithm — Greedy with profit-density scoring:**

1. **Score each package**: `score = Profit / (α × NormalizedVolume + β × NormalizedWeight)` where α, β weight the relative scarcity of volume vs. weight capacity
2. **Sort descending** by score (parallel `Array.Sort` on pre-allocated array)
3. **First-Fit-Decreasing assignment**: iterate sorted packages, assign to the first trip with sufficient remaining capacity
4. **Optimization**: use a sorted structure (or bucketed index) for trips to quickly find fitting trips

**Performance techniques:**
- `ArrayPool<T>.Shared` for temporary arrays
- `Span<T>` for slice operations
- `readonly struct` scoring tuple to avoid boxing
- Optional `Parallel.For` for independent trip processing

#### [NEW] `LocalSearchOptimizer.cs`
**Optional refinement pass (runs if time budget permits):**
- Swap-based local search: try swapping a low-value assigned package with a higher-value unassigned package
- Time-bounded: stops after configurable milliseconds
- Improves greedy result by 1-3% typically

#### [NEW] `PackageScorer.cs`
- Static utility: computes normalized density scores
- Handles edge cases (zero volume, zero weight packages)

---

### Planning Service (`AlzaLogistics.Core/Services/`)

#### [NEW] `PlanningService.cs`
- Orchestrator: validates input → selects strategy based on DayType → executes → returns result
- On `LowDemand` days: simple round-robin assignment (all packages fit)
- On `HighDemand` days: runs `GreedyKnapsackStrategy` + optional `LocalSearchOptimizer`

---

### Data Generation (`AlzaLogistics.Core/DataGeneration/`)

#### [NEW] `PackageGenerator.cs`
- Generates realistic synthetic package data for testing/benchmarking
- Configurable distributions: weight (0.1–30 kg typical), volume (0.001–0.5 m³), profit (50–5000 Kč)
- Deterministic seed for reproducibility

---

### Console Application (`AlzaLogistics.Console/`)

#### [NEW] `Program.cs`
- CLI entry point with argument parsing
- Generates or loads packages → runs planner → prints summary table
- Shows: total profit, capacity utilization %, packages assigned/unassigned, elapsed time

---

### Unit Tests (`AlzaLogistics.Tests/`)

#### [NEW] `PackageTests.cs` — struct behavior, equality, sorting
#### [NEW] `TripTests.cs` — capacity enforcement, TryAdd logic, edge cases
#### [NEW] `GreedyKnapsackStrategyTests.cs` — correctness scenarios:
  - Single trip, single package (fits / doesn't fit)
  - Profit maximization over volume/weight
  - All packages fit (LowDemand equivalent)
  - No packages fit (all oversized)
  - Boundary: package exactly fills trip
  - Large-scale correctness (1000+ packages, verify no constraint violation)
#### [NEW] `LocalSearchOptimizerTests.cs` — improvement validation
#### [NEW] `PlanningServiceTests.cs` — end-to-end orchestration
#### [NEW] `PackageGeneratorTests.cs` — distribution sanity checks

---

### Benchmarks (`AlzaLogistics.Benchmarks/`)

#### [NEW] `PlanningBenchmarks.cs`
- BenchmarkDotNet suite targeting the key performance requirement
- Scenarios: 100K, 250K, 500K packages × 240 trips
- Measures: execution time, memory allocation, GC pressure
- Target: **< 1 second for 100K packages**

---

### Documentation (`docs/`)

#### [NEW] `solution-description.md`
Required written deliverable covering:
1. Problem understanding and modeling decisions
2. Algorithm choice rationale (greedy density + local search)
3. Stated simplifications (no geographic routing, identical trips, etc.)
4. Comparison of alternatives (ILP, genetic algorithms, simulated annealing, branch-and-bound)
5. Performance analysis and benchmark results
