namespace TimeWidget.Application.Weather;

/// <summary>
/// Describes the result of a weather refresh.
/// </summary>
public enum WeatherLoadStatus
{
    /// <summary>
    /// The weather data was loaded successfully.
    /// </summary>
    Success = 0,

    /// <summary>
    /// The current location could not be resolved.
    /// </summary>
    LocationUnavailable = 1,

    /// <summary>
    /// Weather data could not be loaded.
    /// </summary>
    Unavailable = 2
}
