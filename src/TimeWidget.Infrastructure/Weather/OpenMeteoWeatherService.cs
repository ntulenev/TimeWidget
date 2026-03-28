using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using TimeWidget.Application.Abstractions;
using TimeWidget.Domain.Location;
using TimeWidget.Domain.Weather;

namespace TimeWidget.Infrastructure.Weather;

/// <summary>
/// Loads current weather data from the Open-Meteo API.
/// </summary>
public sealed class OpenMeteoWeatherService(HttpClient httpClient) : IWeatherService
{
    private static readonly string TemperatureUnit =
        RegionInfo.CurrentRegion.TwoLetterISORegionName.Equals(
            "US",
            StringComparison.OrdinalIgnoreCase)
            ? "fahrenheit"
            : "celsius";

    private readonly HttpClient _httpClient = httpClient;

    /// <inheritdoc />
    public async Task<WeatherSnapshot> GetCurrentWeatherAsync(
        Coordinates coordinates,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(coordinates);

        var requestPath = FormattableString.Invariant(
            $"v1/forecast?latitude={coordinates.Latitude}&longitude={coordinates.Longitude}&current=temperature_2m,weather_code,is_day&temperature_unit={TemperatureUnit}&timezone=auto");
        var requestUri = new Uri(requestPath, UriKind.Relative);

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var weather = await JsonSerializer.DeserializeAsync<OpenMeteoResponse>(
            responseStream,
            cancellationToken: cancellationToken);

        if (weather?.Current is null)
        {
            throw new InvalidOperationException("Open-Meteo returned an unexpected payload.");
        }

        var roundedTemperature = (int)Math.Round(
            weather.Current.Temperature,
            MidpointRounding.AwayFromZero);
        var condition = GetWeatherLabel(weather.Current.WeatherCode, weather.Current.IsDay == 1);
        var location = BuildLocationLabel(weather, coordinates);

        return new WeatherSnapshot(roundedTemperature, condition, location);
    }

    private static string BuildLocationLabel(OpenMeteoResponse weather, Coordinates coordinates)
    {
        if (!string.IsNullOrWhiteSpace(weather.Timezone))
        {
            var timezoneParts = weather.Timezone.Split('/');
            return timezoneParts[^1].Replace('_', ' ');
        }

        return coordinates.FallbackLabel ?? "Current location";
    }

    private static string GetWeatherLabel(int weatherCode, bool isDay)
    {
        return weatherCode switch
        {
            0 => isDay ? "Clear" : "Clear night",
            1 => "Mostly clear",
            2 => "Partly cloudy",
            3 => "Overcast",
            45 or 48 => "Fog",
            51 or 53 or 55 => "Drizzle",
            56 or 57 => "Freezing drizzle",
            61 or 63 or 65 => "Rain",
            66 or 67 => "Freezing rain",
            71 or 73 or 75 or 77 => "Snow",
            80 or 81 or 82 => "Showers",
            85 or 86 => "Snow showers",
            95 => "Thunderstorm",
            96 or 99 => "Storm hail",
            _ => "Weather"
        };
    }

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("current")]
        public CurrentWeather? Current { get; init; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; init; }
    }

    private sealed class CurrentWeather
    {
        [JsonPropertyName("temperature_2m")]
        public double Temperature { get; init; }

        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; init; }

        [JsonPropertyName("is_day")]
        public int IsDay { get; init; }
    }
}

