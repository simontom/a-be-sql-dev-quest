using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Console;

/// <summary>
/// Holds parsed command-line arguments and resolved execution mode.
/// </summary>
public sealed class CommandLineOptions
{
    public int PackageCount { get; init; } = 200_000;
    public int TripCount { get; init; } = 240;
    public string DayArg { get; init; } = "all";
    public int LocalSearchBudgetMs { get; init; } = 200;
    public DayType TargetDayType { get; init; } = DayType.HighDemand;
    public bool RunBothModes { get; init; } = true;
    public string DayLabel { get; init; } = "All (High + Low Demand comparison)";
    public bool IsHelpRequested { get; init; }
}
