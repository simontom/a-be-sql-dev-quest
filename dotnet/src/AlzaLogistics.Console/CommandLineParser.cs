using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Console;

/// <summary>
/// Parses command-line arguments and displays help information.
/// </summary>
public static class CommandLineParser
{
    private const int DefaultPackageCount = 200_000;
    private const int DefaultTripCount = 240;

    public static CommandLineOptions Parse(string[] args)
    {
        if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h" || args[0] == "/?"))
        {
            return new CommandLineOptions { IsHelpRequested = true };
        }

        var packageCount = args.Length > 0 && int.TryParse(args[0], out var pc) ? pc : DefaultPackageCount;
        var tripCount = args.Length > 1 && int.TryParse(args[1], out var tc) ? tc : DefaultTripCount;
        var dayArg = args.Length > 2 ? args[2] : "all";
        var localSearchBudgetMs = args.Length > 3 && int.TryParse(args[3], out var ls) ? ls : 200;

        var runBoth = dayArg.Equals("all", StringComparison.OrdinalIgnoreCase);
        var targetDayType = DayType.HighDemand;
        var dayLabel = "High Demand (Mon/Wed/Fri/Sat/Sun)";

        if (!runBoth)
        {
            if (DayTypeExtensions.TryParseDayOrDemand(dayArg, out targetDayType))
            {
                dayLabel = targetDayType == DayType.LowDemand
                    ? $"Low Demand (Tue/Thu - selected via '{dayArg}')"
                    : $"High Demand (Mon/Wed/Fri/Sat/Sun - selected via '{dayArg}')";
            }
            else
            {
                System.Console.WriteLine($"[Warning] Unrecognized day/demand '{dayArg}'. Defaulting to High Demand mode.");
            }
        }

        return new CommandLineOptions
        {
            PackageCount = packageCount,
            TripCount = tripCount,
            DayArg = dayArg,
            LocalSearchBudgetMs = localSearchBudgetMs,
            TargetDayType = targetDayType,
            RunBothModes = runBoth,
            DayLabel = runBoth ? "All (High + Low Demand comparison)" : dayLabel,
            IsHelpRequested = false
        };
    }

    public static void PrintHelp()
    {
        System.Console.WriteLine("AlzaLogistics Delivery Planning CLI");
        System.Console.WriteLine();
        System.Console.WriteLine("Usage:");
        System.Console.WriteLine("  dotnet run --project src/AlzaLogistics.Console [packageCount] [tripCount] [day|demand] [localSearchMs]");
        System.Console.WriteLine();
        System.Console.WriteLine("Arguments:");
        System.Console.WriteLine("  [packageCount]    Number of packages to generate and plan (default: 200,000)");
        System.Console.WriteLine("  [tripCount]       Number of delivery trips available (default: 240)");
        System.Console.WriteLine("  [day|demand]      Day of week or demand mode (default: 'all')");
        System.Console.WriteLine("                    Values for Low Demand (Round-Robin):      Tue, Thu, Tuesday, Thursday, Low");
        System.Console.WriteLine("                    Values for High Demand (Greedy Knapsack): Mon, Wed, Fri, Sat, Sun, High, Today");
        System.Console.WriteLine("                    Special value 'all':                       Runs High Demand + Low Demand comparison");
        System.Console.WriteLine("  [localSearchMs]   Time budget in milliseconds for Local Search (default: 200)");
        System.Console.WriteLine();
        System.Console.WriteLine("Examples:");
        System.Console.WriteLine("  dotnet run --project src/AlzaLogistics.Console 50000 100 Tue");
        System.Console.WriteLine("  dotnet run --project src/AlzaLogistics.Console 200000 240 Mon 300");
        System.Console.WriteLine("  dotnet run --project src/AlzaLogistics.Console 100000 240 High");
    }
}
