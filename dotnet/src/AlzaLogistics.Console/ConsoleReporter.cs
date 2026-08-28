using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Console;

/// <summary>
/// Formats and renders console output tables, headers, and planning summaries.
/// </summary>
public static class ConsoleReporter
{
    public static void PrintHeader()
    {
        System.Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║  AlzaLogistics - Optimal Delivery Capacity Planning   ║");
        System.Console.WriteLine("║  Quest 2: Doručení teleportem                         ║");
        System.Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        System.Console.WriteLine();
    }

    public static void PrintConfiguration(CommandLineOptions options)
    {
        System.Console.WriteLine("Configuration:");
        System.Console.WriteLine($"  - Packages:               {options.PackageCount:N0}");
        System.Console.WriteLine($"  - Delivery Trips:         {options.TripCount}");
        System.Console.WriteLine($"  - Mode / Day:             {options.DayLabel}");
        System.Console.WriteLine($"  - Local Search Budget:    {options.LocalSearchBudgetMs} ms");
        System.Console.WriteLine();
    }

    public static void PrintSamplePackages(Package[] packages, int count = 5)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"Sample packages (first {Math.Min(count, packages.Length)}):");
        System.Console.WriteLine($"  {"ID",-8} {"Weight (kg)",-14} {"Volume (m³)",-14} {"Profit (CZK)",-14}");
        System.Console.WriteLine($"  {new string('-', 50)}");
        
        for (var i = 0; i < Math.Min(count, packages.Length); i++)
        {
            var p = packages[i];
            System.Console.WriteLine($"  {p.Id,-8} {p.WeightKg,-14:F3} {p.VolumeM3,-14:F4} {p.ProfitCzk,-14:F2}");
        }
        
        System.Console.WriteLine();
    }

    public static void PrintTopTrips(PlanningResult result, int count = 5)
    {
        var topTrips = result.Trips
            .Where(t => t.PackageCount > 0)
            .OrderByDescending(t => t.TotalProfit)
            .Take(count)
            .ToArray();

        if (topTrips.Length == 0)
        {
            return;
        }

        System.Console.WriteLine();
        System.Console.WriteLine($"Top {topTrips.Length} trips by profit:");
        System.Console.WriteLine($"  {"Trip",-8} {"Packages",-10} {"Profit (CZK)",-16} {"Vol Util",-12} {"Wt Util",-12}");
        System.Console.WriteLine($"  {new string('-', 58)}");

        foreach (var trip in topTrips)
        {
            System.Console.WriteLine($"  {trip.Id,-8} {trip.PackageCount,-10} {trip.TotalProfit,-16:N2} {trip.VolumeUtilization,-12:P1} {trip.WeightUtilization,-12:P1}");
        }
    }
}
