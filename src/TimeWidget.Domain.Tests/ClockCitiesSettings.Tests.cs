using FluentAssertions;

using TimeWidget.Domain.Clock;

namespace TimeWidget.Domain.Tests;

public sealed class ClockCitiesSettingsTests
{
    [Fact(DisplayName = "Get Configured Cities should filter unconfigured entries.")]
    [Trait("Category", "Unit")]
    public void GetConfiguredCitiesShouldFilterUnconfiguredEntries()
    {
        // Arrange
        var settings = new ClockCitiesSettings
        {
            LeftCities =
            [
                new CityClockDefinition { Name = "Berlin", TimeZoneId = TimeZoneInfo.Utc.Id },
                new CityClockDefinition { Name = " ", TimeZoneId = TimeZoneInfo.Utc.Id }
            ],
            RightCities =
            [
                new CityClockDefinition { Name = "UTC", TimeZoneId = TimeZoneInfo.Utc.Id },
                new CityClockDefinition { Name = "Tokyo", TimeZoneId = string.Empty }
            ]
        };

        // Act
        var leftCities = settings.GetConfiguredLeftCities();
        var rightCities = settings.GetConfiguredRightCities();

        // Assert
        leftCities.Should().ContainSingle();
        leftCities[0].Name.Should().Be("Berlin");
        rightCities.Should().ContainSingle();
        rightCities[0].Name.Should().Be("UTC");
    }
}

