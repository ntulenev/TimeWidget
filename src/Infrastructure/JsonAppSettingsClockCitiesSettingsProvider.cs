using System.IO;
using System.Text.Json;

using TimeWidget.Abstractions;
using TimeWidget.Models;

namespace TimeWidget.Infrastructure;

public sealed class JsonAppSettingsClockCitiesSettingsProvider : IClockCitiesSettingsProvider
{
    private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public ClockCitiesSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new ClockCitiesSettings();
            }

            using var stream = File.OpenRead(SettingsFilePath);
            var payload = JsonSerializer.Deserialize<AppSettingsPayload>(stream, SerializerOptions);
            return Sanitize(payload?.ClockCities);
        }
        catch
        {
            return new ClockCitiesSettings();
        }
    }

    private static ClockCitiesSettings Sanitize(ClockCitiesSettings? settings)
    {
        if (settings is null)
        {
            return new ClockCitiesSettings();
        }

        return new ClockCitiesSettings
        {
            LeftCities = Sanitize(settings.LeftCities),
            RightCities = Sanitize(settings.RightCities)
        };
    }

    private static CityClockSettings[] Sanitize(CityClockSettings[]? cities)
    {
        if (cities is null || cities.Length == 0)
        {
            return [];
        }

        return cities
            .Where(city => !string.IsNullOrWhiteSpace(city.Name))
            .Select(city => new CityClockSettings
            {
                Name = city.Name.Trim(),
                TimeZoneId = city.TimeZoneId?.Trim() ?? string.Empty
            })
            .ToArray();
    }

    private sealed class AppSettingsPayload
    {
        public ClockCitiesSettings? ClockCities { get; init; }
    }
}
