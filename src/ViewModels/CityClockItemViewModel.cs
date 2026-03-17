using System.Globalization;

namespace TimeWidget.ViewModels;

public sealed class CityClockItemViewModel : ObservableObject
{
    private readonly TimeZoneInfo? _timeZoneInfo;
    private string _timeText = "--:--";

    public CityClockItemViewModel(string name, string timeZoneId)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Unknown" : name.Trim();
        _timeZoneInfo = TryResolveTimeZone(timeZoneId);
    }

    public string Name { get; }

    public string TimeText
    {
        get => _timeText;
        private set => SetProperty(ref _timeText, value);
    }

    public void Update(DateTimeOffset now, CultureInfo cultureInfo, bool use24HourClock)
    {
        if (_timeZoneInfo is null)
        {
            TimeText = "--:--";
            return;
        }

        var localTime = TimeZoneInfo.ConvertTime(now, _timeZoneInfo);
        TimeText = localTime.ToString(use24HourClock ? "HH:mm" : "hh:mm", cultureInfo);
    }

    private static TimeZoneInfo? TryResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch
        {
            return null;
        }
    }
}
