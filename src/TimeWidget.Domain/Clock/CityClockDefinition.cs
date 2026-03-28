using System.Globalization;

namespace TimeWidget.Domain.Clock;

/// <summary>
/// Describes a configured city clock.
/// </summary>
public sealed class CityClockDefinition
{
    /// <summary>
    /// Gets or sets the display name of the city.
    /// </summary>
    public string Name {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    } = string.Empty;

    /// <summary>
    /// Gets or sets the system time zone identifier for the city.
    /// </summary>
    public string TimeZoneId {
        get;
        set => field = value?.Trim() ?? string.Empty;
    } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the clock has enough information to be used.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(TimeZoneId);

    /// <summary>
    /// Attempts to resolve the configured time zone identifier.
    /// </summary>
    /// <returns>The resolved time zone, or <see langword="null"/> when the identifier is invalid.</returns>
    public TimeZoneInfo? TryResolveTimeZone()
    {
        if (string.IsNullOrWhiteSpace(TimeZoneId))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    /// <summary>
    /// Formats the current time for this city.
    /// </summary>
    /// <param name="now">The current timestamp.</param>
    /// <param name="cultureInfo">The culture used to format the time.</param>
    /// <param name="use24HourClock">A value indicating whether a 24-hour clock should be used.</param>
    /// <returns>The formatted local time, or a placeholder when the time zone cannot be resolved.</returns>
    public string FormatTime(DateTimeOffset now, CultureInfo cultureInfo, bool use24HourClock)
    {
        var timeZone = TryResolveTimeZone();
        if (timeZone is null)
        {
            return "--:--";
        }

        var localTime = TimeZoneInfo.ConvertTime(now, timeZone);
        return localTime.ToString(use24HourClock ? "HH:mm" : "hh:mm", cultureInfo);
    }
}
