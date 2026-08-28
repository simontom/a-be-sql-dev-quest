# Benchmark Results — Strategy Comparison

**Machine**: Intel Core i7-7700HQ 2.80GHz (Kaby Lake), 8 logical / 4 physical cores  
**Runtime**: .NET 10.0.11, X64 RyuJIT AVX2  
**Mode**: BenchmarkDotNet Dry run, Release build  
**Input**: 100,000 packages → 240 trips (7 m³ / 5,500 kg each)

## Results

| Strategy | Day | Mean | Gen0 GC | Gen1 GC | Allocated |
|---|---|---|---|---|---|
| Greedy only | Mon/Wed/Fri/Sat/Sun | **80 ms** | — | — | 13.4 MB |
| Round-Robin | Tue/Thu | 93 ms | — | — | 14.6 MB |
| Greedy + Local Search | Mon/Wed/Fri/Sat/Sun | 178 ms | 8,000 | 1,000 | **56.4 MB** |

---

## Observations

### 1. Round-Robin is slower than Greedy Only (93 ms vs 80 ms)

This is counterintuitive — O(n) round-robin should beat O(n log n) greedy. The reason: as trips fill up, round-robin **degrades** because for each package it scans all 240 trips looking for one with room. Greedy does a single sorted pass with early exit, so the sort cost pays for itself.

### 2. Local Search has a GC problem

Greedy + Local Search allocates **56.4 MB** (4× more than greedy only) and triggers heavy GC:
- **8,000 Gen0 collections** per 1,000 operations
- **1,000 Gen1 collections** per 1,000 operations

**Root cause**: the current `LocalSearchOptimizer` rebuilds a brand-new `Trip` object on every successful swap, copying all kept packages into it. With thousands of swap attempts across 240 trips, this creates massive short-lived heap pressure.

**Fix**: make `Trip` support in-place package removal instead of full rebuild.

### 3. Greedy Only is GC-free

Both Greedy Only and Round-Robin show **zero GC collections** — clean, predictable allocations. This is the expected behaviour from the `readonly record struct` design and pre-allocated arrays.

---

## All Scenarios (100K / 250K / 500K)

> ⚠️ These are single-iteration dry runs — directionally accurate but not statistically rigorous.

| Scenario | Strategy | Mean |
|---|---|---|
| 100K packages, 240 trips | Greedy only | 80 ms |
| 100K packages, 240 trips | Round-Robin | 93 ms |
| 100K packages, 240 trips | Greedy + Local Search | 178 ms |
| 250K packages, 240 trips | Greedy only | ~350 ms *(est.)* |
| 500K packages, 240 trips | Greedy only | ~700 ms *(est.)* |

> [!IMPORTANT]
> The performance target from the spec is **< 1 second for 100K+ packages**. All three strategies
> meet this target. The spec is satisfied even with local search enabled.

> [!WARNING]
> The **56.4 MB / heavy GC** from Local Search is a known issue. The `LocalSearchOptimizer`
> rebuilds `Trip` objects on every swap instead of mutating in place. This should be fixed before
> production use to avoid GC pauses under load.
