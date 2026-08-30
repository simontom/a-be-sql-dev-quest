using System.Text.Json;
using System.Text.Json.Serialization;
using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Core.Services;

public class JsonResultExporter : IResultExporter
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task ExportAsync(PlanningResult result, string targetDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        // Export assigned trips (packed in cars)
        var tripsFile = Path.Combine(targetDirectory, "trips.json");
        await using (var tripsStream = File.Create(tripsFile))
        {
            await JsonSerializer.SerializeAsync(tripsStream, result.Trips, _options, cancellationToken);
        }

        // Export unassigned packages (not packed in cars)
        var unassignedFile = Path.Combine(targetDirectory, "unassigned.json");
        await using (var unassignedStream = File.Create(unassignedFile))
        {
            await JsonSerializer.SerializeAsync(unassignedStream, result.UnassignedPackages, _options, cancellationToken);
        }

        if (result.UnassignedMandatoryPackages.Length > 0)
        {
            var unassignedMandatoryFile = Path.Combine(targetDirectory, "unassigned_mandatory.json");
            await using (var unassignedMandatoryStream = File.Create(unassignedMandatoryFile))
            {
                await JsonSerializer.SerializeAsync(unassignedMandatoryStream, result.UnassignedMandatoryPackages, _options, cancellationToken);
            }
        }

        // Export summary
        var summary = new
        {
            result.AlgorithmName,
            result.TotalPackages,
            AssignedCount = result.AssignedPackageCount,
            UnassignedCount = result.UnassignedPackages.Length,
            UnassignedMandatoryCount = result.UnassignedMandatoryPackages.Length,
            result.TotalProfit,
            TripsUsed = result.Trips.Count(t => t.PackageCount > 0),
            TotalTrips = result.Trips.Length,
            ElapsedMilliseconds = result.ElapsedTime.TotalMilliseconds
        };
        
        var summaryFile = Path.Combine(targetDirectory, "summary.json");
        await using (var summaryStream = File.Create(summaryFile))
        {
            await JsonSerializer.SerializeAsync(summaryStream, summary, _options, cancellationToken);
        }
    }
}
