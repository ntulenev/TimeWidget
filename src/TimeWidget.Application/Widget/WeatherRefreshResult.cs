using TimeWidget.Application.Weather;

namespace TimeWidget.Application.Widget;

public sealed record WeatherRefreshResult(WeatherDisplayState DisplayState, bool Succeeded);
