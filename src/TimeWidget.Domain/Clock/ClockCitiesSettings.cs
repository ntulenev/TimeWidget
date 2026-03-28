using System.Collections.ObjectModel;

namespace TimeWidget.Domain.Clock;

/// <summary>
/// Stores the city clock configuration for the widget.
/// </summary>
public sealed class ClockCitiesSettings
{
    /// <summary>
    /// Gets the city clocks displayed on the left side.
    /// </summary>
    public Collection<CityClockDefinition> LeftCities { get; } = [];

    /// <summary>
    /// Gets the city clocks displayed on the right side.
    /// </summary>
    public Collection<CityClockDefinition> RightCities { get; } = [];

    /// <summary>
    /// Gets the configured left-side city clocks.
    /// </summary>
    /// <returns>The configured left-side clocks.</returns>
    public IReadOnlyList<CityClockDefinition> GetConfiguredLeftCities() =>
        [.. LeftCities.Where(city => city.IsConfigured)];

    /// <summary>
    /// Gets the configured right-side city clocks.
    /// </summary>
    /// <returns>The configured right-side clocks.</returns>
    public IReadOnlyList<CityClockDefinition> GetConfiguredRightCities() =>
        [.. RightCities.Where(city => city.IsConfigured)];
}
