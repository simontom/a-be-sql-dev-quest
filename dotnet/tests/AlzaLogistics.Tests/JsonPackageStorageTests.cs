using AlzaLogistics.Core.Model;
using AlzaLogistics.Core.Services;

namespace AlzaLogistics.Tests;

public class JsonPackageStorageTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly JsonPackageStorage _storage;

    public JsonPackageStorageTests()
    {
        _testFilePath = Path.GetTempFileName();
        _storage = new JsonPackageStorage();
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPersistPackagesCorrectly()
    {
        // Arrange
        var originalPackages = new[]
        {
            new Package(1, 10.5, 0.1, 150m),
            new Package(2, 5.0, 0.05, 75m)
        };

        // Act
        await _storage.SavePackagesAsync(originalPackages, _testFilePath);
        var loadedPackages = await _storage.LoadPackagesAsync(_testFilePath);

        // Assert
        Assert.Equal(2, loadedPackages.Length);
        Assert.Equal(originalPackages[0], loadedPackages[0]);
        Assert.Equal(originalPackages[1], loadedPackages[1]);
    }

    [Fact]
    public async Task Load_NonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _storage.LoadPackagesAsync(invalidPath));
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
}
