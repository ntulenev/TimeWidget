using TimeWidget.Domain.Weather;

namespace TimeWidget.Application.Weather;

/// <summary>
/// Represents the result of loading weather data.
/// </summary>
public sealed class WeatherLoadResult
{
    private WeatherLoadResult(WeatherLoadStatus status, WeatherSnapshot? snapshot)
    {
        Status = status;
        Snapshot = snapshot;
    }

    /// <summary>
    /// Gets the weather load status.
    /// </summary>
    public WeatherLoadStatus Status { get; }

    /// <summary>
    /// Gets the loaded weather snapshot when available.
    /// </summary>
    public WeatherSnapshot? Snapshot { get; }

    /// <summary>
    /// Creates a successful weather result.
    /// </summary>
    /// <param name="snapshot">The loaded weather snapshot.</param>
    /// <returns>A successful weather result.</returns>
    public static WeatherLoadResult Success(WeatherSnapshot snapshot) =>
        new(WeatherLoadStatus.Success, snapshot);

    /// <summary>
    /// Creates a weather result for a non-success status.
    /// </summary>
    /// <param name="status">The resulting status.</param>
    /// <returns>A weather result without a snapshot.</returns>
    public static WeatherLoadResult FromStatus(WeatherLoadStatus status) =>
        new(status, null);
}
