using System.Globalization;

namespace TimeWidget.Domain.Clock;

public sealed class CityClockDefinition
{
    private string _name = string.Empty;
    private string _timeZoneId = string.Empty;

    public string Name
    {
        get => _name;
        set => _name = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public string TimeZoneId
    {
        get => _timeZoneId;
        set => _timeZoneId = value?.Trim() ?? string.Empty;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(TimeZoneId);

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
        catch
        {
            return null;
        }
    }

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
