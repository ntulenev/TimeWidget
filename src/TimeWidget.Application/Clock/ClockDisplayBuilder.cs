using System.Globalization;

using TimeWidget.Domain.Clock;

namespace TimeWidget.Application.Clock;

/// <summary>
/// Converts clock configuration into UI-facing display state.
/// </summary>
public sealed class ClockDisplayBuilder
{
    /// <summary>
    /// Builds the clock display state for the specified moment.
    /// </summary>
    /// <param name="now">The current timestamp.</param>
    /// <param name="settings">The configured city clocks.</param>
    /// <returns>The display state to render.</returns>
    public ClockDisplayState Build(DateTimeOffset now, ClockCitiesSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var currentCulture = CultureInfo.CurrentCulture;
        var dateText = now.ToString("dddd, d MMMM yyyy", _dateCulture);

        return new ClockDisplayState(
            now.ToString(_use24HourClock ? "HH:mm" : "hh:mm", currentCulture),
            string.IsNullOrEmpty(dateText)
                ? string.Empty
                : char.ToUpper(dateText[0], _dateCulture) + dateText[1..],
            [
                .. settings.GetConfiguredLeftCities()
                .Select(city => new CityClockDisplayState(
                    city.Name,
                    city.FormatTime(now, currentCulture, _use24HourClock)))
            ],
            [
                .. settings.GetConfiguredRightCities()
                .Select(city => new CityClockDisplayState(
                    city.Name,
                    city.FormatTime(now, currentCulture, _use24HourClock)))
            ]);
    }

    private readonly CultureInfo _dateCulture = CultureInfo.GetCultureInfo("en-US");
    private readonly bool _use24HourClock = true;
}
