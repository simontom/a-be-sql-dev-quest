using AlzaLogistics.Core.Model;

namespace AlzaLogistics.Tests;

public class DayTypeExtensionsTests
{
    [Theory]
    [InlineData(DayOfWeek.Tuesday, DayType.LowDemand)]
    [InlineData(DayOfWeek.Thursday, DayType.LowDemand)]
    [InlineData(DayOfWeek.Monday, DayType.HighDemand)]
    [InlineData(DayOfWeek.Wednesday, DayType.HighDemand)]
    [InlineData(DayOfWeek.Friday, DayType.HighDemand)]
    [InlineData(DayOfWeek.Saturday, DayType.HighDemand)]
    [InlineData(DayOfWeek.Sunday, DayType.HighDemand)]
    public void GetDayType_ReturnsCorrectDemandLevel(DayOfWeek day, DayType expected)
    {
        Assert.Equal(expected, day.GetDayType());
    }

    [Theory]
    [InlineData("Tue", DayType.LowDemand)]
    [InlineData("Tuesday", DayType.LowDemand)]
    [InlineData("Thu", DayType.LowDemand)]
    [InlineData("Thursday", DayType.LowDemand)]
    [InlineData("Low", DayType.LowDemand)]
    [InlineData("Mon", DayType.HighDemand)]
    [InlineData("Monday", DayType.HighDemand)]
    [InlineData("Wed", DayType.HighDemand)]
    [InlineData("Fri", DayType.HighDemand)]
    [InlineData("Sat", DayType.HighDemand)]
    [InlineData("Sun", DayType.HighDemand)]
    [InlineData("High", DayType.HighDemand)]
    public void TryParseDayOrDemand_ParsesValidInputs(string input, DayType expected)
    {
        var success = DayTypeExtensions.TryParseDayOrDemand(input, out var result);
        Assert.True(success);
        Assert.Equal(expected, result);
    }
}
