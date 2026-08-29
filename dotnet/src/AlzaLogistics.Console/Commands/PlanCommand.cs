using System.Diagnostics;
using AlzaLogistics.Core.DataGeneration;
using AlzaLogistics.Core.Model;
using AlzaLogistics.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AlzaLogistics.Console.Commands;

public class PlanCommand : AsyncCommand<PlanSettings>
{
    private readonly IPackageStorage _packageStorage;

    public PlanCommand()
    {
        _packageStorage = new JsonPackageStorage();
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, PlanSettings settings, CancellationToken cancellationToken)
    {
        ConsoleReporter.PrintHeader();
        
        Package[] packages;
        if (!string.IsNullOrWhiteSpace(settings.InputFilePath) && File.Exists(settings.InputFilePath))
        {
            AnsiConsole.MarkupLine($"[blue]Loading packages from {settings.InputFilePath}...[/]");
            var loadSw = Stopwatch.StartNew();
            packages = await _packageStorage.LoadPackagesAsync(settings.InputFilePath);
            loadSw.Stop();
            AnsiConsole.MarkupLine($"[blue]Loaded {packages.Length:N0} packages in {loadSw.ElapsedMilliseconds} ms[/]");
            
            if (packages.Length > settings.PackageCount)
            {
                packages = packages.Take(settings.PackageCount).ToArray();
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]No valid input file provided. Generating {settings.PackageCount:N0} packages on the fly...[/]");
            var genSw = Stopwatch.StartNew();
            var generator = new PackageGenerator();
            packages = generator.Generate(settings.PackageCount);
            genSw.Stop();
            AnsiConsole.MarkupLine($"[yellow]Generated in {genSw.ElapsedMilliseconds} ms[/]");
        }

        ConsoleReporter.PrintSamplePackages(packages);

        var service = new PlanningService();
        var runBoth = settings.DayArg.Equals("all", StringComparison.OrdinalIgnoreCase);
        
        if (!runBoth)
        {
            if (!DayTypeExtensions.TryParseDayOrDemand(settings.DayArg, out var targetDayType))
            {
                AnsiConsole.MarkupLine($"[red]Unrecognized day/demand '{settings.DayArg}'. Defaulting to High Demand mode.[/]");
                targetDayType = DayType.HighDemand;
            }

            var dayLabel = targetDayType == DayType.LowDemand
                    ? $"Low Demand (Tue/Thu - selected via '{settings.DayArg}')"
                    : $"High Demand (Mon/Wed/Fri/Sat/Sun - selected via '{settings.DayArg}')";

            ConsoleReporter.PrintConfiguration(packages.Length, settings.TripCount, dayLabel, settings.LocalSearchBudgetMs);

            AnsiConsole.MarkupLine($"[green]Executing planning for {dayLabel}...[/]");
            var request = new PlanningRequest
            {
                Packages = packages,
                TripCount = settings.TripCount,
                DayType = targetDayType,
                LocalSearchTimeBudgetMs = targetDayType == DayType.HighDemand ? settings.LocalSearchBudgetMs : 0
            };

            var result = service.Plan(request);
            ConsoleReporter.PrintResultSummary(result);
            ConsoleReporter.PrintTopTrips(result);
        }
        else
        {
            ConsoleReporter.PrintConfiguration(packages.Length, settings.TripCount, "All (High + Low Demand comparison)", settings.LocalSearchBudgetMs);

            // Run High Demand planning
            AnsiConsole.MarkupLine($"[green]Planning {packages.Length:N0} packages across {settings.TripCount} trips (High Demand mode)...[/]");
            
            var request = new PlanningRequest
            {
                Packages = packages,
                TripCount = settings.TripCount,
                DayType = DayType.HighDemand,
                LocalSearchTimeBudgetMs = settings.LocalSearchBudgetMs
            };

            var result = service.Plan(request);
            ConsoleReporter.PrintResultSummary(result);
            ConsoleReporter.PrintTopTrips(result);

            AnsiConsole.WriteLine();

            // Run Low Demand comparison
            AnsiConsole.MarkupLine($"[green]Planning same packages in Low Demand mode (Tue/Thu - Round-Robin)...[/]");
            var lowRequest = new PlanningRequest
            {
                Packages = packages,
                TripCount = settings.TripCount,
                DayType = DayType.LowDemand,
                LocalSearchTimeBudgetMs = 0
            };

            var lowResult = service.Plan(lowRequest);
            ConsoleReporter.PrintResultSummary(lowResult);
        }

        return 0;
    }
}
