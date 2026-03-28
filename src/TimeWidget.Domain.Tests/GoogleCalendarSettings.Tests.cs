using FluentAssertions;

using TimeWidget.Domain.Configuration;

namespace TimeWidget.Domain.Tests;

public sealed class GoogleCalendarSettingsTests
{
    [Fact(DisplayName = "Constructor should initialize expected defaults.")]
    [Trait("Category", "Unit")]
    public void CtorShouldInitializeExpectedDefaults()
    {
        // Arrange
        var settings = new GoogleCalendarSettings();

        // Act
        // Assert
        settings.Enabled.Should().BeTrue();
        settings.CalendarId.Should().Be("primary");
        settings.Mode.Should().Be(GoogleCalendarMode.Compact);
        settings.MaxEventsCompact.Should().Be(3);
        settings.MaxEventsFull.Should().Be(8);
        settings.RefreshMinutes.Should().Be(5);
        settings.ClientSecretsPath.Should().NotBeNullOrWhiteSpace();
        settings.TokenStoreDirectory.Should().NotBeNullOrWhiteSpace();
        settings.LoginHint.Should().BeNull();
        settings.ForceAccountSelection.Should().BeTrue();
        settings.IsFullCalendarMode.Should().BeFalse();
    }

    [Theory(DisplayName = "Active Max Events should depend on configured mode.")]
    [Trait("Category", "Unit")]
    [InlineData(GoogleCalendarMode.Compact, 3)]
    [InlineData(GoogleCalendarMode.FullCalendar, 8)]
    public void ActiveMaxEventsShouldDependOnConfiguredMode(GoogleCalendarMode mode, int expectedMaxEvents)
    {
        // Arrange
        var settings = new GoogleCalendarSettings
        {
            Mode = mode,
            MaxEventsCompact = 3,
            MaxEventsFull = 8
        };

        // Act
        // Assert
        settings.ActiveMaxEvents.Should().Be(expectedMaxEvents);
        settings.IsFullCalendarMode.Should().Be(mode == GoogleCalendarMode.FullCalendar);
    }
}


