namespace TimeWidget.Application.Weather;

/// <summary>
/// Converts weather load results into UI-facing display state.
/// </summary>
public sealed class WeatherDisplayBuilder
{
    /// <summary>
    /// Builds the weather display state.
    /// </summary>
    /// <param name="result">The weather load result.</param>
    /// <returns>The display state to render.</returns>
    public WeatherDisplayState Build(WeatherLoadResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

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
                _locationUnavailableText,
                false),
            WeatherLoadStatus.Unavailable => new WeatherDisplayState(
                string.Empty,
                string.Empty,
                _weatherUnavailableText,
                false),
            _ => new WeatherDisplayState(
                string.Empty,
                string.Empty,
                _weatherUnavailableText,
                false)
        };
    }

    private readonly string _locationUnavailableText = "Enable Windows location";
    private readonly string _weatherUnavailableText = "Weather unavailable";
}
