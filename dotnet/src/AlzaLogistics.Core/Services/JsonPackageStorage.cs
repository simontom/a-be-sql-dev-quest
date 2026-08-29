using System.Text.Json;
using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Core.Services;

/// <summary>
/// Implementation of IPackageStorage using System.Text.Json.
/// </summary>
public class JsonPackageStorage : IPackageStorage
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SavePackagesAsync(IEnumerable<Package> packages, string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, packages, _options, cancellationToken);
    }

    public async Task<Package[]> LoadPackagesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Package file not found: {filePath}");
        }

        await using var stream = File.OpenRead(filePath);
        var packages = await JsonSerializer.DeserializeAsync<Package[]>(stream, _options, cancellationToken);
        
        return packages ?? Array.Empty<Package>();
    }
}
