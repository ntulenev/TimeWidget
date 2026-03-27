using System.Globalization;

using TimeWidget.Domain.Clock;

namespace TimeWidget.Application.Clock;

public sealed class ClockDisplayBuilder
{
    private static readonly CultureInfo DateCulture = CultureInfo.GetCultureInfo("en-US");
    private const bool Use24HourClock = true;

    public ClockDisplayState Build(DateTimeOffset now, ClockCitiesSettings settings)
    {
        var currentCulture = CultureInfo.CurrentCulture;
        var dateText = now.ToString("dddd, d MMMM yyyy", DateCulture);

        return new ClockDisplayState(
            now.ToString(Use24HourClock ? "HH:mm" : "hh:mm", currentCulture),
            string.IsNullOrEmpty(dateText)
                ? string.Empty
                : char.ToUpper(dateText[0], DateCulture) + dateText[1..],
            settings.GetConfiguredLeftCities()
                .Select(city => new CityClockDisplayState(
                    city.Name,
                    city.FormatTime(now, currentCulture, Use24HourClock)))
                .ToArray(),
            settings.GetConfiguredRightCities()
                .Select(city => new CityClockDisplayState(
                    city.Name,
                    city.FormatTime(now, currentCulture, Use24HourClock)))
                .ToArray());
    }
}
