namespace TimeWidget.Models;

public sealed class ClockCitiesSettings
{
    public CityClockSettings[] LeftCities { get; set; } = [];

    public CityClockSettings[] RightCities { get; set; } = [];
}
