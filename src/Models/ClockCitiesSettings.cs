namespace TimeWidget.Models;

public sealed class ClockCitiesSettings
{
    public CityClockSettings[] LeftCities { get; init; } = [];

    public CityClockSettings[] RightCities { get; init; } = [];
}
