using TimeWidget.Models;

namespace TimeWidget.Abstractions;

public interface IWeatherService
{
    Task<WeatherInfo> GetCurrentWeatherAsync(
        Coordinates coordinates,
        CancellationToken cancellationToken);
}

