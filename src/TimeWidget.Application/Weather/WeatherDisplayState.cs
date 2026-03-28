namespace TimeWidget.Application.Weather;

/// <summary>
/// Represents the weather section state shown in the widget.
/// </summary>
/// <param name="TemperatureText">The formatted temperature text.</param>
/// <param name="ConditionText">The weather condition text.</param>
/// <param name="LocationText">The location label.</param>
/// <param name="HasDetails">A value indicating whether weather details are available.</param>
public sealed record WeatherDisplayState(
    string TemperatureText,
    string ConditionText,
    string LocationText,
    bool HasDetails);
