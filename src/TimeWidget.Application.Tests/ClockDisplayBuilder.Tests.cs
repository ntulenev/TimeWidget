using System.Globalization;

using FluentAssertions;

using TimeWidget.Application.Clock;
using TimeWidget.Domain.Clock;

namespace TimeWidget.Application.Tests;

public sealed class ClockDisplayBuilderTests
{
    [Fact(DisplayName = "Build should return clock and configured cities.")]
    [Trait("Category", "Unit")]
    public void BuildShouldReturnClockAndConfiguredCities()
    {
        // Arrange
        var builder = new ClockDisplayBuilder();
        var now = new DateTimeOffset(2026, 3, 28, 9, 45, 0, TimeSpan.Zero);
        var localCity = new CityClockDefinition
        {
            Name = "Local",
            TimeZoneId = TimeZoneInfo.Local.Id
        };
        var utcCity = new CityClockDefinition
        {
            Name = "UTC",
            TimeZoneId = TimeZoneInfo.Utc.Id
        };
        var settings = new ClockCitiesSettings
        {
            LeftCities =
            [
                localCity,
                new CityClockDefinition { Name = " ", TimeZoneId = TimeZoneInfo.Utc.Id }
            ],
            RightCities = [utcCity]
        };

        // Act
        var state = builder.Build(now, settings);

        // Assert
        state.TimeText.Should().Be(now.ToString("HH:mm", CultureInfo.CurrentCulture));
        state.DateText.Should().Be("Saturday, 28 March 2026");
        state.LeftCityTimes.Should().ContainSingle();
        state.LeftCityTimes[0].Name.Should().Be("Local");
        state.LeftCityTimes[0].TimeText.Should().Be(localCity.FormatTime(now, CultureInfo.CurrentCulture, true));
        state.RightCityTimes.Should().ContainSingle();
        state.RightCityTimes[0].Name.Should().Be("UTC");
        state.RightCityTimes[0].TimeText.Should().Be(utcCity.FormatTime(now, CultureInfo.CurrentCulture, true));
    }

    [Fact(DisplayName = "Build should exclude unconfigured cities.")]
    [Trait("Category", "Unit")]
    public void BuildShouldExcludeUnconfiguredCities()
    {
        // Arrange
        var builder = new ClockDisplayBuilder();
        var settings = new ClockCitiesSettings
        {
            LeftCities = [new CityClockDefinition { Name = "Paris", TimeZoneId = " " }],
            RightCities = []
        };

        // Act
        var state = builder.Build(DateTimeOffset.UtcNow, settings);

        // Assert
        state.LeftCityTimes.Should().BeEmpty();
        state.RightCityTimes.Should().BeEmpty();
    }
}

