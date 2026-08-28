using AlzaLogistics.Core.Algorithms;
using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Core.Services;

/// <summary>
/// Orchestrates the planning process: validates input, selects the appropriate
/// strategy based on day type, and executes the plan.
/// </summary>
public sealed class PlanningService(IPlanningStrategy? highDemandStrategy = null )
{
    private readonly IPlanningStrategy _highDemandStrategy = highDemandStrategy ?? new GreedyKnapsackStrategy();

    public PlanningResult Plan(PlanningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Packages);

        if (request.Packages.Length == 0)
        {
            return new PlanningResult
            {
                Trips = CreateEmptyTrips(request),
                UnassignedPackages = [],
                TotalPackages = 0,
                ElapsedTime = TimeSpan.Zero,
                AlgorithmName = "None (empty input)"
            };
        }

        return request.DayType switch
        {
            DayType.LowDemand => PlanLowDemand(request),
            DayType.HighDemand => _highDemandStrategy.Plan(request),
            _ => throw new ArgumentOutOfRangeException(nameof(request.DayType))
        };
    }

    /// <summary>
    /// Simple round-robin assignment when capacity is sufficient for all packages.
    /// </summary>
    private static PlanningResult PlanLowDemand(PlanningRequest request)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var trips = CreateTrips(request);
        var unassigned = new List<Package>();
        var tripIdx = 0;

        for (var i = 0; i < request.Packages.Length; i++)
        {
            var placed = false;
            for (var attempt = 0; attempt < trips.Length; attempt++)
            {
                var idx = (tripIdx + attempt) % trips.Length;
                if (trips[idx].TryAdd(in request.Packages[i]))
                {
                    tripIdx = (idx + 1) % trips.Length;
                    placed = true;
                    break;
                }
            }
            if (!placed)
                unassigned.Add(request.Packages[i]);
        }

        sw.Stop();
        return new PlanningResult
        {
            Trips = trips,
            UnassignedPackages = unassigned.ToArray(),
            TotalPackages = request.Packages.Length,
            ElapsedTime = sw.Elapsed,
            AlgorithmName = "Round-Robin (Low Demand)"
        };
    }

    private static Trip[] CreateTrips(PlanningRequest request)
    {
        var trips = new Trip[request.TripCount];
        for (var i = 0; i < request.TripCount; i++)
            trips[i] = new Trip(i, request.TripMaxVolumeM3, request.TripMaxWeightKg);
        return trips;
    }

    private static Trip[] CreateEmptyTrips(PlanningRequest request)
        => CreateTrips(request);
}
