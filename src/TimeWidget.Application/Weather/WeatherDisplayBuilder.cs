namespace TimeWidget.Application.Weather;

public sealed class WeatherDisplayBuilder
{
    public WeatherDisplayState Build(WeatherLoadResult result)
    {
        return result.Status switch
        {
            WeatherLoadStatus.Success when result.Snapshot is not null => new WeatherDisplayState(
                $"{result.Snapshot.Temperature}\u00B0",
                result.Snapshot.Condition,
                result.Snapshot.Location,
                true),
            WeatherLoadStatus.LocationUnavailable => new WeatherDisplayState(
                string.Empty,
                string.Empty,
                "Enable Windows location",
                false),
            _ => new WeatherDisplayState(
                string.Empty,
                string.Empty,
                "Weather unavailable",
                false)
        };
    }
}
