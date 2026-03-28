using Microsoft.Extensions.Options;

using TimeWidget.Domain.Clock;

namespace TimeWidget.Infrastructure.Configuration;

/// <summary>
/// Normalizes configured city clock settings after binding.
/// </summary>
public sealed class ClockCitiesSettingsConfiguration : IPostConfigureOptions<ClockCitiesSettings>
{
    /// <inheritdoc />
    public void PostConfigure(string? name, ClockCitiesSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.LeftCities = NormalizeCities(options.LeftCities);
        options.RightCities = NormalizeCities(options.RightCities);
    }

    private static CityClockDefinition[] NormalizeCities(CityClockDefinition[]? cities)
    {
        if (cities is null || cities.Length == 0)
        {
            return [];
        }

        return
        [
            .. cities
            .Select(city => new CityClockDefinition
            {
                Name = city.Name,
                TimeZoneId = city.TimeZoneId
            })
            .Where(city => city.IsConfigured)
        ];
    }
}
