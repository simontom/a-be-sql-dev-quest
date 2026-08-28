namespace AlzaLogistics.Core.Model;

/// <summary>
/// Represents the demand level for a given day.
/// LowDemand (Tue/Thu): all packages fit within fleet capacity.
/// HighDemand (Mon/Wed/Fri/Sat/Sun): overcapacity, optimization required.
/// </summary>
public enum DayType
{
    LowDemand,
    HighDemand
}
