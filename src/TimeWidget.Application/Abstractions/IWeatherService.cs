using TimeWidget.Domain.Location;
using TimeWidget.Domain.Weather;

namespace TimeWidget.Application.Abstractions;

public interface IWeatherService
{
    Task<WeatherSnapshot> GetCurrentWeatherAsync(
        Coordinates coordinates,
        CancellationToken cancellationToken);
}
