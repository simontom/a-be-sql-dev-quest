using AlzaLogistics.Console.Commands;

namespace AlzaLogistics.Tests;

public class PlanSettingsTests
{
    [Fact]
    public void PlanSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new PlanSettings();

        // Assert
        Assert.Equal(200_000, settings.PackageCount);
        Assert.Equal(240, settings.TripCount);
        Assert.Equal("All", settings.DayArg);
        Assert.Equal(200, settings.LocalSearchBudgetMs);
        Assert.Equal(string.Empty, settings.InputFilePath);
        Assert.Equal(string.Empty, settings.ExportDirectory);
    }
}
