namespace TimeWidget.Application.Weather;

public sealed record WeatherDisplayState(
    string TemperatureText,
    string ConditionText,
    string LocationText,
    bool HasDetails);
