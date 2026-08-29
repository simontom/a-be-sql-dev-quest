using System.Text.Json;
using AlzaLogistics.Core.Model;
using AlzaLogistics.Core.Services;

namespace AlzaLogistics.Tests;

public class JsonResultExporterTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly JsonResultExporter _exporter;

    public JsonResultExporterTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _exporter = new JsonResultExporter();
    }

    [Fact]
    public async Task ExportAsync_ShouldCreateFiles()
    {
        // Arrange
        var package1 = new Package(1, 10, 0.5, 100);
        var package2 = new Package(2, 5, 0.2, 50);

        var trip = new Trip(1, 10, 100);
        trip.TryAdd(package1);

        var result = new PlanningResult
        {
            Trips = new[] { trip },
            UnassignedPackages = new[] { package2 },
            TotalPackages = 2,
            AlgorithmName = "TestAlgorithm",
            ElapsedTime = TimeSpan.FromMilliseconds(10)
        };

        // Act
        await _exporter.ExportAsync(result, _testDirectory);

        // Assert
        Assert.True(Directory.Exists(_testDirectory));
        Assert.True(File.Exists(Path.Combine(_testDirectory, "trips.json")));
        Assert.True(File.Exists(Path.Combine(_testDirectory, "unassigned.json")));
        Assert.True(File.Exists(Path.Combine(_testDirectory, "summary.json")));

        var summaryJson = await File.ReadAllTextAsync(Path.Combine(_testDirectory, "summary.json"));
        using var doc = JsonDocument.Parse(summaryJson);
        Assert.Equal("TestAlgorithm", doc.RootElement.GetProperty("algorithmName").GetString());
        Assert.Equal(2, doc.RootElement.GetProperty("totalPackages").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("unassignedCount").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("assignedCount").GetInt32());
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
