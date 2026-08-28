namespace AlzaLogistics.Core.Model;

/// <summary>
/// Input for the planning service: packages to allocate, fleet configuration, and day type.
/// </summary>
public sealed class PlanningRequest
{
    public required Package[] Packages { get; init; }
    public int TripCount { get; init; } = 240;
    public double TripMaxVolumeM3 { get; init; } = Trip.DefaultMaxVolumeM3;
    public double TripMaxWeightKg { get; init; } = Trip.DefaultMaxWeightKg;
    public DayType DayType { get; init; } = DayType.HighDemand;

    /// <summary>
    /// Maximum milliseconds allowed for local search refinement. 0 = skip local search.
    /// </summary>
    public int LocalSearchTimeBudgetMs { get; init; } = 100;
}
