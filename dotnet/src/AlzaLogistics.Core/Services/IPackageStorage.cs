using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Core.Services;

/// <summary>
/// Provides mechanism to persist and load packages.
/// </summary>
public interface IPackageStorage
{
    /// <summary>
    /// Saves a collection of packages to the specified file.
    /// </summary>
    Task SavePackagesAsync(IEnumerable<Package> packages, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a collection of packages from the specified file.
    /// </summary>
    Task<Package[]> LoadPackagesAsync(string filePath, CancellationToken cancellationToken = default);
}
