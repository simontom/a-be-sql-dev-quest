using System.Diagnostics;
using AlzaLogistics.Console;
using AlzaLogistics.Core.DataGeneration;
using AlzaLogistics.Core.Model;
using AlzaLogistics.Core.Services;

var options = CommandLineParser.Parse(args);

if (options.IsHelpRequested)
{
    CommandLineParser.PrintHelp();
    return;
}

ConsoleReporter.PrintHeader();
ConsoleReporter.PrintConfiguration(options);

// Generate synthetic packages
Console.Write($"Generating {options.PackageCount:N0} packages... ");
var genSw = Stopwatch.StartNew();
var generator = new PackageGenerator();
var packages = generator.Generate(options.PackageCount);
genSw.Stop();
Console.WriteLine($"done in {genSw.ElapsedMilliseconds} ms");

ConsoleReporter.PrintSamplePackages(packages);

var service = new PlanningService();

if (!options.RunBothModes)
{
    // Run single requested day/mode
    Console.WriteLine($"Executing planning for {options.DayLabel}...");
    var request = new PlanningRequest
    {
        Packages = packages,
        TripCount = options.TripCount,
        DayType = options.TargetDayType,
        LocalSearchTimeBudgetMs = options.TargetDayType == DayType.HighDemand ? options.LocalSearchBudgetMs : 0
    };

    var result = service.Plan(request);
    Console.Write(result.GetSummary());
    ConsoleReporter.PrintTopTrips(result);
}
else
{
    // Run High Demand planning
    Console.WriteLine($"Planning {options.PackageCount:N0} packages across {options.TripCount} trips (High Demand mode)...");
    Console.WriteLine();

    var request = new PlanningRequest
    {
        Packages = packages,
        TripCount = options.TripCount,
        DayType = DayType.HighDemand,
        LocalSearchTimeBudgetMs = options.LocalSearchBudgetMs
    };

    var result = service.Plan(request);
    Console.Write(result.GetSummary());
    ConsoleReporter.PrintTopTrips(result);

    Console.WriteLine();

    // Run Low Demand comparison
    Console.WriteLine($"Planning same packages in Low Demand mode (Tue/Thu - Round-Robin)...");
    var lowRequest = new PlanningRequest
    {
        Packages = packages,
        TripCount = options.TripCount,
        DayType = DayType.LowDemand,
        LocalSearchTimeBudgetMs = 0
    };

    var lowResult = service.Plan(lowRequest);
    Console.Write(lowResult.GetSummary());
}
