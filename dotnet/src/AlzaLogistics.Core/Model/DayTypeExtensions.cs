namespace AlzaLogistics.Core.Model;

/// <summary>
/// Extensions and helper methods for mapping days of the week to demand levels (DayType).
/// </summary>
public static class DayTypeExtensions
{
    /// <summary>
    /// Gets the demand level based on business rules:
    /// - Tuesday and Thursday: LowDemand (sufficient capacity for all packages).
    /// - Monday, Wednesday, Friday, Saturday, Sunday: HighDemand (capacity shortage, optimization required).
    /// </summary>
    public static DayType GetDayType(this DayOfWeek day) => day switch
    {
        DayOfWeek.Tuesday or DayOfWeek.Thursday => DayType.LowDemand,
        _ => DayType.HighDemand
    };

    /// <summary>
    /// Parses a string representation of a day of the week or demand level into a DayType.
    /// Supports English day names/abbreviations (Mon, Tue, ...), Czech day names/abbreviations (Po, Út, ...),
    /// demand levels (High, Low), or "today".
    /// </summary>
    public static bool TryParseDayOrDemand(string? input, out DayType dayType)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            dayType = DayType.HighDemand;
            return false;
        }

        var clean = input.Trim().ToLowerInvariant();

        if (clean == "today")
        {
            dayType = DateTime.Now.DayOfWeek.GetDayType();
            return true;
        }

        if (clean is "low" or "lowdemand" or "út" or "ut" or "tue" or "tuesday" or "čt" or "ct" or "thu" or "thursday")
        {
            dayType = DayType.LowDemand;
            return true;
        }

        if (clean is "high" or "highdemand" or "po" or "mon" or "monday" or "st" or "wed" or "wednesday"
                   or "pá" or "pa" or "fri" or "friday" or "so" or "sat" or "saturday" or "ne" or "sun" or "sunday")
        {
            dayType = DayType.HighDemand;
            return true;
        }

        if (Enum.TryParse<DayOfWeek>(input, ignoreCase: true, out var dayOfWeek))
        {
            dayType = dayOfWeek.GetDayType();
            return true;
        }

        if (Enum.TryParse<DayType>(input, ignoreCase: true, out var parsedType))
        {
            dayType = parsedType;
            return true;
        }

        dayType = DayType.HighDemand;
        return false;
    }
}
