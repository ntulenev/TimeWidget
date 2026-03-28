using System.Globalization;

using FluentAssertions;

using TimeWidget.Domain.Clock;

namespace TimeWidget.Domain.Tests;

public sealed class CityClockDefinitionTests
{
    [Fact(DisplayName = "Properties should trim assigned values.")]
    [Trait("Category", "Unit")]
    public void PropertiesShouldTrimAssignedValues()
    {
        // Arrange
        var city = new CityClockDefinition
        {
            Name = " Berlin ",
            TimeZoneId = $" {TimeZoneInfo.Utc.Id} "
        };

        // Act
        // Assert
        city.Name.Should().Be("Berlin");
        city.TimeZoneId.Should().Be(TimeZoneInfo.Utc.Id);
    }

    [Fact(DisplayName = "Is Configured should return true when name and time zone are set.")]
    [Trait("Category", "Unit")]
    public void IsConfiguredShouldReturnTrueWhenNameAndTimeZoneAreSet()
    {
        // Arrange
        var city = new CityClockDefinition
        {
            Name = "Berlin",
            TimeZoneId = TimeZoneInfo.Utc.Id
        };

        // Act
        // Assert
        city.IsConfigured.Should().BeTrue();
    }

    [Fact(DisplayName = "Try Resolve Time Zone should return null for unknown time zone.")]
    [Trait("Category", "Unit")]
    public void TryResolveTimeZoneShouldReturnNullForUnknownTimeZone()
    {
        // Arrange
        var city = new CityClockDefinition
        {
            Name = "Berlin",
            TimeZoneId = "Unknown/Zone"
        };

        // Act
        var timeZone = city.TryResolveTimeZone();

        // Assert
        timeZone.Should().BeNull();
    }

    [Fact(DisplayName = "Format Time should return placeholder when time zone cannot be resolved.")]
    [Trait("Category", "Unit")]
    public void FormatTimeShouldReturnPlaceholderWhenTimeZoneCannotBeResolved()
    {
        // Arrange
        var city = new CityClockDefinition
        {
            Name = "Berlin",
            TimeZoneId = string.Empty
        };

        // Act
        var formatted = city.FormatTime(DateTimeOffset.UtcNow, CultureInfo.InvariantCulture, use24HourClock: true);

        // Assert
        formatted.Should().Be("--:--");
    }

    [Fact(DisplayName = "Format Time should format time for resolved time zone.")]
    [Trait("Category", "Unit")]
    public void FormatTimeShouldFormatTimeForResolvedTimeZone()
    {
        // Arrange
        var city = new CityClockDefinition
        {
            Name = "UTC",
            TimeZoneId = TimeZoneInfo.Utc.Id
        };
        var now = new DateTimeOffset(2026, 3, 28, 9, 15, 0, TimeSpan.Zero);

        // Act
        var formatted = city.FormatTime(now, CultureInfo.InvariantCulture, use24HourClock: true);

        // Assert
        formatted.Should().Be("09:15");
    }
}

