using System.ComponentModel;
using AlzaLogistics.Core.Model;
using Spectre.Console.Cli;

namespace AlzaLogistics.Console.Commands;

public class PlanSettings : CommandSettings
{
    [CommandOption("-i|--input")]
    [Description("Path to load the generated packages from (e.g. packages.json).")]
    public string InputFilePath { get; set; } = string.Empty;

    [CommandOption("-p|--packages")]
    [Description("Number of packages to use from the generated pool.")]
    [DefaultValue(200_000)]
    public int PackageCount { get; set; } = 200_000;

    [CommandOption("-t|--trips")]
    [Description("Number of delivery trips available.")]
    [DefaultValue(240)]
    public int TripCount { get; set; } = 240;

    [CommandOption("-d|--day")]
    [Description("Day of week or demand mode ('High', 'Low', 'Mon', 'Tue', etc. or 'All').")]
    [DefaultValue("All")]
    public string DayArg { get; set; } = "All";

    [CommandOption("-l|--local-search-ms")]
    [Description("Time budget in milliseconds for Local Search.")]
    [DefaultValue(200)]
    public int LocalSearchBudgetMs { get; set; } = 200;

    [CommandOption("-o|--export-dir")]
    [Description("Directory path to export the planning results (trips, unassigned packages, summary).")]
    public string ExportDirectory { get; set; } = string.Empty;
}
