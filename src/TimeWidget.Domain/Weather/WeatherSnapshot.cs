namespace TimeWidget.Domain.Weather;

/// <summary>
/// Represents the current weather at a location.
/// </summary>
/// <param name="Temperature">The current temperature.</param>
/// <param name="Condition">The weather condition text.</param>
/// <param name="Location">The location label.</param>
public sealed record WeatherSnapshot(int Temperature, string Condition, string Location);
