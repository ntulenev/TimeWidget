using FluentAssertions;

using TimeWidget.Application.Weather;
using TimeWidget.Domain.Weather;

namespace TimeWidget.Application.Tests;

public sealed class WeatherDisplayBuilderTests
{
    [Fact(DisplayName = "Build should return display state for successful weather.")]
    [Trait("Category", "Unit")]
    public void BuildShouldReturnDisplayStateForSuccessfulWeather()
    {
        // Arrange
        var builder = new WeatherDisplayBuilder();
        var result = WeatherLoadResult.Success(new WeatherSnapshot(22, "Sunny", "Berlin"));

        // Act
        var state = builder.Build(result);

        // Assert
        state.TemperatureText.Should().Be($"22\u00B0");
        state.ConditionText.Should().Be("Sunny");
        state.LocationText.Should().Be("Berlin");
        state.HasDetails.Should().BeTrue();
    }

    [Fact(DisplayName = "Build should show location message when coordinates unavailable.")]
    [Trait("Category", "Unit")]
    public void BuildShouldShowLocationMessageWhenCoordinatesUnavailable()
    {
        // Arrange
        var builder = new WeatherDisplayBuilder();

        // Act
        var state = builder.Build(WeatherLoadResult.FromStatus(WeatherLoadStatus.LocationUnavailable));

        // Assert
        state.TemperatureText.Should().BeEmpty();
        state.ConditionText.Should().BeEmpty();
        state.LocationText.Should().Be("Enable Windows location");
        state.HasDetails.Should().BeFalse();
    }

    [Fact(DisplayName = "Build should show unavailable message for non success statuses.")]
    [Trait("Category", "Unit")]
    public void BuildShouldShowUnavailableMessageForNonSuccessStatuses()
    {
        // Arrange
        var builder = new WeatherDisplayBuilder();

        // Act
        var state = builder.Build(WeatherLoadResult.FromStatus(WeatherLoadStatus.Unavailable));

        // Assert
        state.LocationText.Should().Be("Weather unavailable");
        state.HasDetails.Should().BeFalse();
    }
}


