using AlzaLogistics.Core.Model;
using Spectre.Console;

namespace AlzaLogistics.Console;

/// <summary>
/// Formats and renders console output tables, headers, and planning summaries using Spectre.Console.
/// </summary>
public static class ConsoleReporter
{
    public static void PrintHeader()
    {
        AnsiConsole.Write(
            new FigletText("AlzaLogistics")
                .Centered()
                .Color(Color.Blue));
                
        AnsiConsole.MarkupLine("[bold blue]Quest 2: Doručení teleportem[/]");
        AnsiConsole.WriteLine();
    }

    public static void PrintConfiguration(int packageCount, int tripCount, string dayLabel, int localSearchBudgetMs)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        
        grid.AddRow("[bold]Configuration:[/]", "");
        grid.AddRow("  - Packages:", $"{packageCount:N0}");
        grid.AddRow("  - Delivery Trips:", $"{tripCount}");
        grid.AddRow("  - Mode / Day:", $"{dayLabel}");
        grid.AddRow("  - Local Search Budget:", $"{localSearchBudgetMs} ms");
        
        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();
    }

    public static void PrintSamplePackages(Package[] packages, int count = 5)
    {
        AnsiConsole.WriteLine();
        var table = new Table();
        table.Title($"Sample packages (first {Math.Min(count, packages.Length)})");
        table.AddColumn("ID");
        table.AddColumn("Weight (kg)");
        table.AddColumn("Volume (m³)");
        table.AddColumn(new TableColumn("Profit (CZK)").RightAligned());

        for (var i = 0; i < Math.Min(count, packages.Length); i++)
        {
            var p = packages[i];
            table.AddRow(
                p.Id.ToString(), 
                p.WeightKg.ToString("F3"), 
                p.VolumeM3.ToString("F4"), 
                p.ProfitCzk.ToString("N2")
            );
        }
        
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public static void PrintResultSummary(PlanningResult result)
    {
        var panel = new Panel(result.GetSummary())
        {
            Header = new PanelHeader("Planning Summary"),
            Expand = true
        };
        AnsiConsole.Write(panel);
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

        AnsiConsole.WriteLine();
        
        var table = new Table();
        table.Title($"Top {topTrips.Length} trips by profit");
        table.AddColumn("Trip");
        table.AddColumn(new TableColumn("Packages").Centered());
        table.AddColumn(new TableColumn("Profit (CZK)").RightAligned());
        table.AddColumn("Vol Util");
        table.AddColumn("Wt Util");

        foreach (var trip in topTrips)
        {
            table.AddRow(
                trip.Id.ToString(), 
                trip.PackageCount.ToString(), 
                $"[green]{trip.TotalProfit:N2}[/]", 
                $"{trip.VolumeUtilization:P1}", 
                $"{trip.WeightUtilization:P1}"
            );
        }
        
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}
