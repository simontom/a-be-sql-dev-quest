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

After the greedy pass, an optional local search optimizer attempts profit-improving swaps:
- For each unassigned high-profit package, find a trip where replacing the lowest-profit assigned 
  package yields a net profit gain while respecting 2D constraints.
- **Empirical finding**: As shown in [profitability_analysis.md](file:///d:/Work/Coding/a-be-sql-dev-quest/dotnet/docs/profitability_analysis.md), the greedy phase already achieves **97.0% – 98.6% of the theoretical LP relaxation upper bound**. Consequently, local search yields negligible profit gain (+7.83 CZK on 200k packages) while consuming an extra ~100 ms of CPU time. In a production setting, greedy alone is the recommended configuration.

### Operational Demand Regimes (Tue/Thu vs Peak Days)

The assignment identifies two real-world operational regimes:
1. **Low Demand (Tue/Thu)**: Fleet capacity exceeds demand; all packages fit naturally without contention. The greedy algorithm trivially assigns all valid packages with 100% service rate.
2. **Peak Days (Mon, Wed, Fri, Sat, Sun)**: Demand exceeds capacity; profit-density maximization is crucial to select the highest-yield package subset.

*(Note: While a round-robin mode was initially drafted for low-demand days, the universal greedy algorithm naturally and optimally handles both regimes without requiring separate business logic branching.)*

---

## 3. Simplifications and Architecture Considerations

### Geographic Routing & Route Partitioning

In real-world enterprise logistics (such as Alza's delivery network), planning operates across **three decoupled pipeline stages**:

```
[ Stage 1: Route Partitioning ]  -->  [ Stage 2: Capacity Optimization ]  -->  [ Stage 3: Trip Sequencing ]
  Group packages by AlzaBox /           (OUR MODULE)                            Calculate physical stop
  postal zone & assign van pool         MDMKP: Profit, Priority, Aging          order via TSP/VRP solver
```

1. **Stage 1 — Geographic Zoning & Route Partitioning**: Packages are partitioned by destination region (e.g., *Prague 4*, *Brno-Center*, or specific AlzaBox clusters) and assigned a sub-fleet of vans.
2. **Stage 2 — Capacity Optimization (Our Module)**: When demand in a zone exceeds vehicle capacity, our **Multi-Dimensional Multi-Container Knapsack Planner (MDMKP)** decides which packages are loaded today versus deferred, maximizing profit while honoring business SLAs and preventing starvation.
3. **Stage 3 — Route Sequencing (TSP / VRP)**: Once the winning packages are selected for a specific van, a routing engine (e.g., OSRM or a Traveling Salesperson solver) calculates the physical turn-by-turn stop sequence.

#### Why Decouple Knapsack from Routing?
Combining street-level addresses, turn-by-turn distance matrices, and capacity planning turns the problem into a monolithic **Capacitated Vehicle Routing Problem (CVRP)**. CVRP is computationally intractable for 200,000+ packages within a sub-second budget. 

Decoupling route partitioning into independent knapsack sub-problems ($K$ zones) allows each zone's capacity to be solved concurrently in parallel via `PlanningService.Plan()`:
```csharp
var zoneResults = packages
    .GroupBy(p => p.ZoneId)
    .AsParallel()
    .Select(g => planningService.Plan(new PlanningRequest {
        Packages = g.ToArray(),
        TripCount = zoneTripCounts[g.Key]
    }));
```

### Other Simplifications and Constraints

1. **Identical trips**: All 240 trips are treated as interchangeable bins with equal capacity (7 m³, 5,500 kg).
2. **Single-pass daily allocation**: Packages are allocated in one planning run per day.
3. **Deterministic execution**: Pure heuristic scoring + introsort ensures reproducible planning results.
4. **Positive profits**: All profits are positive as per specification.
5. **Separation of State**: The planner is purely functional; state tracking (`DaysWaiting++`) is managed by the outer database/workflow layer.

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

### Profitability Analysis — Core Business Criterion (Výnosnost)

The primary evaluation criterion of the assignment is the **achieved profitability (yield)** under demand pressure.
Below is the empirical evaluation across heuristic baselines and the **theoretical upper bound (LP Relaxation)** for 200,000 packages (240 trips, 1,680 m³, 1,320,000 kg capacity):

| Strategy | Total Profit (CZK) | % of Theoretical LP Bound | Volume Util % | Weight Util % | Assigned Packages | Execution Time |
|---|---|---|---|---|---|---|
| **1. Baseline: Pure Profit (desc)** | 18,663,553.85 CZK | 15.29% | 100.0% | 8.5% | 4,648 | ~838 ms |
| **2. Baseline: Profit / Weight** | 14,937,656.37 CZK | 12.24% | 100.0% | 0.1% | 7,325 | ~707 ms |
| **3. Baseline: Profit / Volume** | 114,290,176.02 CZK | 93.62% | 83.6% | 56.1% | 64,299 | ~672 ms |
| **4. Balanced Greedy: Profit / (0.5V + 0.5W)** ⭐ | **118,479,506.35 CZK** | **97.06%** | **93.8%** | **54.9%** | **66,532** | **~613 ms** |
| **5. Tuned Ratio: Profit / (0.8V + 0.2W)** | 115,157,329.85 CZK | 94.33% | 85.4% | 55.8% | 64,767 | ~615 ms |
| **6. Balanced + Local Search (100ms)** | 118,479,514.18 CZK | 97.06% | 93.8% | 54.9% | 66,532 | ~894 ms |
| **7. Balanced + SLA/Aging Boost** | 101,850,935.40 CZK | 83.43% | 94.6% | 45.7% | 57,200 | ~798 ms |
| **Theoretical Upper Bound (LP Relaxation)** | **122,074,375.83 CZK** | **100.00%** | *100.0%* | *—* | — | *exact bound* |

> 📊 *For full multi-dataset results (100k & 200k), methodology, and detailed discussion, see [profitability_analysis.md](file:///d:/Work/Coding/a-be-sql-dev-quest/dotnet/docs/profitability_analysis.md).*

#### Key Takeaways:
1. **+534% Profit over Naive Baseline**: Sorting by pure profit selects large bulky items, producing only 18.66M CZK. Normalized multi-dimensional density scoring yields 118.48M CZK.
2. **Proximity to Theoretical Optimum**: The chosen greedy strategy achieves **97.06% (200k) to 98.57% (100k)** of the theoretical LP relaxation upper bound, proving that complex metaheuristics cannot yield substantial profit gains on this distribution.
3. **Empirical Justification for $\alpha = \beta = 0.5$**: Even though volume is the primary bottleneck, skewed weights (e.g. 0.8 / 0.2) reduce total profit by ~3.3M CZK because symmetric normalized costs prevent dense packages from prematurely exhausting trip weight limits.
4. **Local Search Redundancy**: Because greedy packing is already within ~3% of the absolute upper bound, 1-for-1 swaps yield virtually zero marginal profit (+7.83 CZK on 200k packages), justifying omitting local search in production.
5. **Cost of SLA Guarantees**: Enforcing mandatory delivery of flagged packages reduces gross profit by ~14% (from 118.5M to 101.9M CZK), providing transparent trade-off metrics for business operations.

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

- **66 unit tests** covering all components (xUnit)
- **Test categories**:
  - Struct behavior and zero-allocation semantics
  - Capacity enforcement (volume & weight bounds)
  - Algorithm correctness (greedy density scoring, First-Fit-Decreasing)
  - Priority tiers (`Mandatory` phase bypass vs. `Standard`/`Elevated`)
  - Starvation prevention & aging boost escalation
  - Local Search optimizer correctness (in-place swap, mandatory package preservation)
  - Edge cases (oversized packages, `TripCount = 0`, negative `DaysWaiting`, exact capacity fit, deterministic tie-breaking)
  - Constraint integrity at scale (5K–10K packages)
- **BenchmarkDotNet suite**: 4 scenarios (100K/250K/500K packages) with memory diagnostics
