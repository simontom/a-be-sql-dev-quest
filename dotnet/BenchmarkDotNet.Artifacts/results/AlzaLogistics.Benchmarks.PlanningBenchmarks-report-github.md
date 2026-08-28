```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.19045.7663/22H2/2022Update)
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2


```
| Method                                               | Mean      | Error    | StdDev   | Gen0      | Gen1      | Allocated |
|----------------------------------------------------- |----------:|---------:|---------:|----------:|----------:|----------:|
| &#39;100K packages, 240 trips, greedy only&#39;              |  31.02 ms | 0.602 ms | 0.740 ms |  875.0000 |  562.5000 |  11.77 MB |
| &#39;100K packages, 240 trips, greedy + local search&#39;    | 132.88 ms | 1.771 ms | 1.570 ms |  666.6667 |  333.3333 |  11.77 MB |
| &#39;250K packages, 240 trips, greedy only&#39;              |  91.93 ms | 1.774 ms | 2.429 ms | 1333.3333 | 1000.0000 |  34.93 MB |
| &#39;500K packages, 240 trips, greedy only&#39;              | 403.04 ms | 2.623 ms | 2.048 ms | 1000.0000 |         - |  67.06 MB |
| &#39;100K packages, 240 trips, round-robin (Low Demand)&#39; |  84.60 ms | 1.441 ms | 1.348 ms |  333.3333 |         - |   14.6 MB |
