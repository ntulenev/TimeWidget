using TimeWidget.Application.Weather;

namespace TimeWidget.Application.Widget;

/// <summary>
/// Represents the result of refreshing weather data for the widget.
/// </summary>
/// <param name="DisplayState">The display state that should be shown after the refresh.</param>
/// <param name="Succeeded">A value indicating whether the refresh produced live weather data.</param>
public sealed record WeatherRefreshResult(WeatherDisplayState DisplayState, bool Succeeded);
