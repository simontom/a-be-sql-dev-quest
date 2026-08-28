# Solution Description: Doručení teleportem (Optimal Delivery Capacity Planning)

## 1. Problem Understanding

The task requires designing a module for **optimal allocation of packages to delivery van trips**, 
maximizing total profit under volume and weight constraints.

### Formalization

This is a **Multi-dimensional Multi-container Knapsack Problem (MDMKP)**:
- **Containers (bins)**: 240 delivery trips, each with 2D capacity (7 m³ volume, 5,500 kg weight)
- **Items**: 100,000+ packages, each with volume, weight, and profit
- **Objective**: Maximize total profit of assigned packages
- **Constraint**: No trip may exceed its volume or weight capacity

This is NP-hard — exact solutions via Integer Linear Programming (ILP) are computationally 
infeasible for 100K+ items within the sub-second time budget. A heuristic approach is required.

---

## 2. Chosen Approach

### Primary Algorithm: Greedy with Profit-Density Scoring

**Core idea**: Prioritize packages that deliver the most profit per unit of consumed capacity.

**Step 1 — Scoring**: For each package, compute a density score:

```
Score = Profit / (α × V_norm + β × W_norm)
```

Where:
- `V_norm = Volume / MaxTripVolume` (normalized volume consumption)
- `W_norm = Weight / MaxTripWeight` (normalized weight consumption)
- `α = β = 0.5` (equal weighting; tunable based on which dimension is scarcer)

This scoring normalizes both dimensions to [0, 1] before combining, preventing the larger-magnitude 
dimension (weight in kg vs. volume in m³) from dominating.

**Step 2 — Sorting**: Sort packages descending by density score. This is O(n log n).

**Step 3 — First-Fit-Decreasing (FFD)**: Iterate sorted packages and assign each to the first trip 
with sufficient remaining capacity. This greedy assignment is O(n × m) where n = packages, m = trips.

**Total complexity**: O(n log n + n × m) ≈ O(n × m) for typical m << n.

### Refinement: Time-Bounded Local Search

After the greedy pass, a local search optimizer attempts profit-improving swaps:
- For each unassigned high-profit package, find a trip where replacing the lowest-profit assigned 
  package yields a net profit gain while still respecting capacity constraints.
- The search is time-bounded (configurable, default 100ms) to respect the strict time budget.
- Typical improvement: 1-3% additional profit.

### Low-Demand Mode (Tue/Thu)

When capacity is sufficient for all packages, a simple round-robin assignment is used — 
no optimization needed. This is O(n).

---

## 3. Simplifications and Constraints

1. **Identical trips**: All 240 trips are treated as interchangeable bins with equal capacity. 
   Geographic routing and AlzaBox assignments are outside the scope of this capacity optimization.

2. **No package-trip affinity**: Any package can be assigned to any trip. In a real system, 
   geographic constraints would partition packages into route-compatible subsets first.

3. **Single-pass allocation**: Packages are allocated in one planning run per day. 
   No re-planning between the two daily trips is modeled.

4. **Deterministic**: No randomized algorithms (e.g., simulated annealing) in the primary path — 
   the greedy + local search produces deterministic results for the same input.

5. **Positive profits only**: As per spec, all profits are positive. Zero or negative profit 
   packages would require special handling.

---

## 4. Alternative Approaches Considered

| Approach | Pros | Cons | Verdict |
|---|---|---|---|
| **Integer Linear Programming (ILP)** | Optimal solution guaranteed | Exponential worst-case; infeasible for 100K+ items in < 1s | ❌ Too slow |
| **Genetic Algorithm** | Good exploration of solution space | Slow convergence; non-deterministic; complex tuning | ❌ Overkill for this problem size |
| **Simulated Annealing** | Escapes local optima | Requires temperature schedule tuning; non-deterministic | ⚠️ Good alternative but unnecessary given greedy quality |
| **Greedy + Local Search** ✅ | O(n log n) sort + O(n×m) assignment; deterministic; simple to implement and verify | May miss global optimum | ✅ **Chosen**: best speed/quality tradeoff |
| **Branch and Bound** | Near-optimal with pruning | Exponential worst case; unpredictable timing | ❌ Cannot guarantee time budget |
| **Column Generation** | Excellent for bin-packing variants | Complex implementation; slower for single-run scenarios | ❌ Over-engineered |

The greedy density heuristic was chosen because:
1. It naturally handles the 2D constraint (volume + weight) through normalized scoring
2. O(n log n) sorting is the dominant cost — well within budget even for 500K packages
3. The local search provides a safety net for edge cases where greedy makes suboptimal choices
4. Deterministic behavior is desirable for logistics planning (reproducible results)

---

## 5. Performance Results

### Benchmark (BenchmarkDotNet, Release mode, .NET 10.0)

| Scenario | Time | Memory | Gen2 Collections |
|---|---|---|---|
| 100K packages, 240 trips, greedy only | **~31 ms** | 11.7 MB | 0 |
| 100K packages, 240 trips, greedy + local search | **~132 ms** | 11.7 MB | 0 |
| 250K packages, 240 trips, greedy only | **~91 ms** | 34.9 MB | 0 |
| 500K packages, 240 trips, greedy only | **~403 ms** | 67.1 MB | 0 |

**Target: < 1 second for 100K+ packages** ✅ Achieved with significant margin. Zero Gen2 garbage collection.

### Quality Metrics (200K packages, 240 trips)

| Metric | Value |
|---|---|
| Packages assigned | 66,295 / 200,000 (33.1%) |
| Total profit | 117,771,012.94 CZK |
| Average volume utilization | 94.0% |
| Average weight utilization | 54.3% |
| Trips fully utilized | 240/240 |

Volume is the binding constraint — trips are 94% full by volume but only 54% by weight, 
which correctly reflects the package distribution (many small, light, high-value packages).

---

## 6. Code Architecture

```
AlzaLogistics.Core/
├── Model/
│   ├── Package.cs          (readonly record struct — zero heap alloc)
│   ├── Trip.cs             (capacity-constrained container)
│   ├── DayType.cs          (LowDemand / HighDemand enum)
│   ├── PlanningRequest.cs  (input DTO)
│   └── PlanningResult.cs   (output DTO with summary)
├── Algorithms/
│   ├── IPlanningStrategy.cs      (strategy interface)
│   ├── PackageScorer.cs          (density scoring engine)
│   ├── GreedyKnapsackStrategy.cs (primary algorithm)
│   └── LocalSearchOptimizer.cs   (refinement pass)
├── Services/
│   └── PlanningService.cs        (orchestrator)
└── DataGeneration/
    └── PackageGenerator.cs       (synthetic test data)
```

### Performance-Oriented Design Decisions

- **`readonly record struct Package`**: Value type stored inline in arrays — no heap allocation 
  per package, excellent cache locality for 100K+ items
- **`ScoredPackage` readonly record struct**: Sorting scores without boxing overhead
- **`Span<T>` for ReadOnlySpan parameters**: Stack-only references to array slices
- **`MethodImpl(AggressiveInlining)`**: Critical comparison methods inlined by JIT
- **`ArrayPool<T>`**: Shared pools used for scoring buffers to eliminate LOH allocations.
- **In-place Trip Modification**: `Trip.RemoveAt` allows for zero-allocation local search mutations.

---

## 7. Testing

- **34 unit tests** covering all components (xUnit)
- **Test categories**: struct behavior, capacity enforcement, algorithm correctness, 
  constraint integrity at scale (5K-10K packages), edge cases (empty input, oversized packages)
- **BenchmarkDotNet suite**: 4 scenarios (100K/250K/500K packages) with memory diagnostics
