using TimeWidget.Domain.Location;
using TimeWidget.Domain.Weather;

namespace TimeWidget.Application.Abstractions;

/// <summary>
/// Loads weather data for a given location.
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// Gets the current weather for the provided coordinates.
    /// </summary>
    /// <param name="coordinates">The target coordinates.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The current weather snapshot.</returns>
    Task<WeatherSnapshot> GetCurrentWeatherAsync(
        Coordinates coordinates,
        CancellationToken cancellationToken);
}
