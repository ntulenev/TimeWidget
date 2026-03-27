namespace TimeWidget.Domain.Weather;

public sealed record WeatherSnapshot(int Temperature, string Condition, string Location);
