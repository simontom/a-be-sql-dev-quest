using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Core.Services;

/// <summary>
/// Exports planning results to an external format.
/// </summary>
public interface IResultExporter
{
    /// <summary>
    /// Exports the specified planning result to the target directory.
    /// </summary>
    Task ExportAsync(PlanningResult result, string targetDirectory, CancellationToken cancellationToken = default);
}
