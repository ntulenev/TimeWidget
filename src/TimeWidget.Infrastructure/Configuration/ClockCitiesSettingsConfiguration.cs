using System.Collections.ObjectModel;

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

        NormalizeCities(options.LeftCities);
        NormalizeCities(options.RightCities);
    }

    private static void NormalizeCities(Collection<CityClockDefinition> cities)
    {
        ArgumentNullException.ThrowIfNull(cities);

        var normalizedCities = cities
            .Select(city => new CityClockDefinition
            {
                Name = city.Name,
                TimeZoneId = city.TimeZoneId
            })
            .Where(city => city.IsConfigured)
            .ToArray();

        cities.Clear();
        foreach (var city in normalizedCities)
        {
            cities.Add(city);
        }
    }
}
