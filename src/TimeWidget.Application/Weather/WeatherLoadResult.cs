using TimeWidget.Domain.Weather;

namespace TimeWidget.Application.Weather;

public sealed class WeatherLoadResult
{
    private WeatherLoadResult(WeatherLoadStatus status, WeatherSnapshot? snapshot)
    {
        Status = status;
        Snapshot = snapshot;
    }

    public WeatherLoadStatus Status { get; }

    public WeatherSnapshot? Snapshot { get; }

    public static WeatherLoadResult Success(WeatherSnapshot snapshot) =>
        new(WeatherLoadStatus.Success, snapshot);

    public static WeatherLoadResult FromStatus(WeatherLoadStatus status) =>
        new(status, null);
}
