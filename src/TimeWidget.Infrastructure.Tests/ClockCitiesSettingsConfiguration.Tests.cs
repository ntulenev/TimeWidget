using FluentAssertions;

using Microsoft.Extensions.Options;

using TimeWidget.Domain.Clock;
using TimeWidget.Infrastructure.Configuration;

namespace TimeWidget.Infrastructure.Tests;

public sealed class ClockCitiesSettingsConfigurationTests
{
    [Fact(DisplayName = "Post Configure should throw when options is null.")]
    [Trait("Category", "Unit")]
    public void PostConfigureShouldThrowWhenOptionsIsNull()
    {
        // Arrange
        var configuration = new ClockCitiesSettingsConfiguration();

        // Act
        var action = () => configuration.PostConfigure(Options.DefaultName, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Post Configure should clone and filter configured cities.")]
    [Trait("Category", "Unit")]
    public void PostConfigureShouldCloneAndFilterConfiguredCities()
    {
        // Arrange
        var configuration = new ClockCitiesSettingsConfiguration();
        var originalCity = new CityClockDefinition
        {
            Name = " Berlin ",
            TimeZoneId = $" {TimeZoneInfo.Utc.Id} "
        };
        var options = new ClockCitiesSettings
        {
            LeftCities =
            {
                originalCity,
                new CityClockDefinition { Name = string.Empty, TimeZoneId = TimeZoneInfo.Utc.Id }
            }
        };

        // Act
        configuration.PostConfigure(Options.DefaultName, options);

        // Assert
        options.LeftCities.Should().ContainSingle();
        options.LeftCities[0].Name.Should().Be("Berlin");
        options.LeftCities[0].TimeZoneId.Should().Be(TimeZoneInfo.Utc.Id);
        options.LeftCities[0].Should().NotBeSameAs(originalCity);
    }
}

