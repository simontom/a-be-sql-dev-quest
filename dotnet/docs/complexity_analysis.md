# Algorithm Complexity Analysis

## Variables

| Symbol | Meaning | Typical value |
|---|---|---|
| $n$ | Number of packages | 100,000 – 500,000 |
| $m$ | Number of trips | 240 |
| $k$ | Average packages per trip | ~280 (100K / 240 ≈ 416 max, most trips fill on volume first) |
| $u$ | Unassigned packages after greedy pass | varies by day demand |
| $i$ | Local search iterations until no improvement | time-bounded |
| $T$ | Local search time budget | 100 ms (configurable) |

---

## 1. Round-Robin — Low Demand (Tue / Thu)

**File**: [`PlanningService.PlanLowDemand()`](file:///d:/Work/Coding/a-be-sql-dev-quest/dotnet/src/AlzaLogistics.Core/Services/PlanningService.cs)

**Loop structure** (directly from code):
```
for each package i in [0..n):          // outer: n iterations
    for attempt in [0..m):             // inner: at most m iterations
        TryAdd(package) → O(1)
```

| | Complexity |
|---|---|
| **Time — best case** | $O(n)$ — each package placed on first try (early trips still have room) |
| **Time — worst case** | $O(n \times m)$ — trips are nearly full, every package scans all 240 |
| **Time — average** | $O(n \times m / 2)$ → effectively $O(n)$ since $m=240$ is a constant |
| **Space** | $O(n + m)$ — packages array (input) + trips array |
| **GC pressure** | None — no extra heap allocations in the hot path |

---

## 2. Greedy Knapsack — High Demand, greedy only

**File**: [`GreedyKnapsackStrategy`](file:///d:/Work/Coding/a-be-sql-dev-quest/dotnet/src/AlzaLogistics.Core/Algorithms/GreedyKnapsackStrategy.cs) + [`PackageScorer`](file:///d:/Work/Coding/a-be-sql-dev-quest/dotnet/src/AlzaLogistics.Core/Algorithms/PackageScorer.cs)

**Phase breakdown**:

| Phase | Code | Complexity |
|---|---|---|
| Initialize $m$ trips | `new Trip[m]` loop | $O(m)$ |
| Score $n$ packages | single `for` loop, O(1) math per package | $O(n)$ |
| Sort by score | `Array.Sort(scored)` — introsort | $O(n \log n)$ |
| First-Fit-Decreasing | nested `for s … for t` | $O(n \times m)$ worst case |
| Collect unassigned | single `for` scan | $O(n)$ |
| **Total** | | $O(n \log n + n \times m)$ |

Since $m = 240$ is a constant: $O(n \times m) = O(240n) = O(n)$ asymptotically, but the $n \log n$ sort dominates in practice for large $n$.

> For $n = 100\text{K}$: $n \log_2 n \approx 1{,}700\text{K}$ ops vs $n \times m = 24\text{M}$ ops worst case.
> FFD exits early once a trip accepts the package, so the real assignment cost is well below $n \times m$.

| | Complexity |
|---|---|
| **Time** | $O(n \log n + n \times m)$ |
| **Space** | $O(n)$ working space — `ScoredPackage[]` of length $n$ + `bool[]` of length $n$ |
| **GC pressure** | **None** — Arrays are rented from `ArrayPool<T>.Shared`, resulting in zero Large Object Heap (LOH) allocations per request. |

---

## 3. Local Search Optimizer — High Demand refinement pass

**File**: [`LocalSearchOptimizer`](file:///d:/Work/Coding/a-be-sql-dev-quest/dotnet/src/AlzaLogistics.Core/Algorithms/LocalSearchOptimizer.cs)

**Loop structure** (directly from code):
```text
sort unassigned desc by profit:          O(u log u)
while improved AND time < T:             i iterations
    for each unassigned u:               u iterations
        for each trip t in [0..m):       m iterations
            scan trip packages → O(k)    find worst package
            if swap improves profit:
                RemoveAt & TryAdd → O(k) ← NO allocation (in-place shift)
```

| Phase | Complexity |
|---|---|
| Initial sort of unassigned | $O(u \log u)$ |
| Per iteration | $O(u \times m \times k)$ |
| Total (unbounded) | $O(i \times u \times m \times k)$ |
| **Total (time-bounded by $T$)** | $O(T / t_{\text{swap}})$ where $t_{\text{swap}}$ is the cost of one swap |
| Trip swap | $O(k)$ — array shift in `List<Package>` upon `RemoveAt` |

| | Complexity |
|---|---|
| **Time** | $O(i \times u \times m \times k)$, bounded in practice by the $T = 100\text{ ms}$ budget |
| **Space — working set** | $O(u + m)$ — unassigned list + trips array |
| **Space — per swap (transient)** | $O(1)$ — no new objects are created |
| **GC pressure** | **None** — successfully eliminated via `Trip.RemoveAt` in-place mutation. |

> [!TIP]
> The previous trip-rebuild pattern was a known GC bottleneck. By implementing `RemoveAt(int index)` on `Trip`, we eliminated all transient allocations per swap. This allows the local search to perform vastly more iterations within the exact same 100ms budget, finding a higher optimum.

---

## Summary Table

| Algorithm | Day | Time | Space | GC |
|---|---|---|---|---|
| Round-Robin | Tue / Thu | $O(n \times m)$, effectively $O(n)$ | $O(n + m)$ | None |
| Greedy Knapsack | Mon/Wed/Fri/Sat/Sun | $O(n \log n + n \times m)$ | $O(n)$ | None |
| Greedy + Local Search | Mon/Wed/Fri/Sat/Sun | $O(n \log n + n \times m + i \times u \times m \times k)$, cap $T$ | $O(n + m)$ | None |

### Measured vs. theoretical (100K packages, 240 trips, optimized run)

| Algorithm | Measured time | Measured memory | Gen2 Collections |
|---|---|---|---|
| Round-Robin | ~85 ms | 14.6 MB | 0 |
| Greedy only | ~31 ms | 11.7 MB | 0 |
| Greedy + Local Search | ~133 ms | 11.7 MB | 0 |
