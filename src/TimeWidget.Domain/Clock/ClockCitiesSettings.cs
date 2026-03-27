namespace TimeWidget.Domain.Clock;

public sealed class ClockCitiesSettings
{
    public CityClockDefinition[] LeftCities { get; set; } = [];

    public CityClockDefinition[] RightCities { get; set; } = [];

    public IReadOnlyList<CityClockDefinition> GetConfiguredLeftCities() =>
        LeftCities.Where(city => city.IsConfigured).ToArray();

    public IReadOnlyList<CityClockDefinition> GetConfiguredRightCities() =>
        RightCities.Where(city => city.IsConfigured).ToArray();
}
