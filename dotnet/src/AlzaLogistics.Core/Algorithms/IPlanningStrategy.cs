using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Core.Algorithms;

/// <summary>
/// Strategy interface for package-to-trip allocation algorithms.
/// </summary>
public interface IPlanningStrategy
{
    string Name { get; }
    PlanningResult Plan(PlanningRequest request);
}
