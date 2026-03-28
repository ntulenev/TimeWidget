using System.Net;
using System.Text;

using FluentAssertions;

using TimeWidget.Domain.Location;
using TimeWidget.Infrastructure.Weather;

namespace TimeWidget.Infrastructure.Tests;

public sealed class OpenMeteoWeatherServiceTests
{
    [Fact(DisplayName = "Get Current Weather should map payload to snapshot.")]
    [Trait("Category", "Unit")]
    public async Task GetCurrentWeatherAsyncShouldMapPayloadToSnapshot()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "current": {
                    "temperature_2m": 12.6,
                    "weather_code": 0,
                    "is_day": 0
                  },
                  "timezone": "Europe/Berlin"
                }
                """,
                Encoding.UTF8,
                "application/json")
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.open-meteo.com/")
        };
        var service = new OpenMeteoWeatherService(httpClient);

        // Act
        var snapshot = await service.GetCurrentWeatherAsync(
            new Coordinates(52.52, 13.40, "Berlin"),
            cts.Token);

        // Assert
        snapshot.Temperature.Should().Be(13);
        snapshot.Condition.Should().Be("Clear night");
        snapshot.Location.Should().Be("Berlin");
    }

    [Fact(DisplayName = "Get Current Weather should use fallback location when timezone missing.")]
    [Trait("Category", "Unit")]
    public async Task GetCurrentWeatherAsyncShouldUseFallbackLocationWhenTimezoneMissing()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "current": {
                    "temperature_2m": 3.1,
                    "weather_code": 95,
                    "is_day": 1
                  }
                }
                """,
                Encoding.UTF8,
                "application/json")
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.open-meteo.com/")
        };
        var service = new OpenMeteoWeatherService(httpClient);

        // Act
        var snapshot = await service.GetCurrentWeatherAsync(
            new Coordinates(40.71, -74.01, "New York"),
            cts.Token);

        // Assert
        snapshot.Temperature.Should().Be(3);
        snapshot.Condition.Should().Be("Thunderstorm");
        snapshot.Location.Should().Be("New York");
    }

    [Fact(DisplayName = "Get Current Weather should throw when payload does not contain current weather.")]
    [Trait("Category", "Unit")]
    public async Task GetCurrentWeatherAsyncShouldThrowWhenPayloadDoesNotContainCurrentWeather()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"timezone":"Europe/Berlin"}""", Encoding.UTF8, "application/json")
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.open-meteo.com/")
        };
        var service = new OpenMeteoWeatherService(httpClient);

        // Act
        var action = async () => await service.GetCurrentWeatherAsync(
            new Coordinates(52.52, 13.40, "Berlin"),
            cts.Token);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Open-Meteo returned an unexpected payload.");
    }

    private sealed class TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}

