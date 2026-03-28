using FluentAssertions;

using Microsoft.Extensions.Options;

using TimeWidget.Domain.Configuration;
using TimeWidget.Infrastructure.Configuration;

namespace TimeWidget.Infrastructure.Tests;

public sealed class GoogleCalendarSettingsConfigurationTests
{
    [Fact(DisplayName = "Post Configure should throw when options is null.")]
    [Trait("Category", "Unit")]
    public void PostConfigureShouldThrowWhenOptionsIsNull()
    {
        // Arrange
        var configuration = new GoogleCalendarSettingsConfiguration();

        // Act
        var action = () => configuration.PostConfigure(Options.DefaultName, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Post Configure should normalize and clamp values.")]
    [Trait("Category", "Unit")]
    public void PostConfigureShouldNormalizeAndClampValues()
    {
        // Arrange
        var configuration = new GoogleCalendarSettingsConfiguration();
        var options = new GoogleCalendarSettings
        {
            CalendarId = " ",
            Mode = (GoogleCalendarMode)99,
            MaxEventsCompact = 0,
            MaxEventsFull = 99,
            RefreshMinutes = 0,
            ClientSecretsPath = "  client.json  ",
            TokenStoreDirectory = "  tokens  ",
            LoginHint = "  "
        };

        // Act
        configuration.PostConfigure(Options.DefaultName, options);

        // Assert
        options.CalendarId.Should().Be("primary");
        options.Mode.Should().Be(GoogleCalendarMode.Compact);
        options.MaxEventsCompact.Should().Be(1);
        options.MaxEventsFull.Should().Be(20);
        options.RefreshMinutes.Should().Be(1);
        options.ClientSecretsPath.Should().Be("client.json");
        options.TokenStoreDirectory.Should().Be("tokens");
        options.LoginHint.Should().BeNull();
    }

    [Fact(DisplayName = "Validate should fail when enabled and calendar id is missing.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldFailWhenEnabledAndCalendarIdIsMissing()
    {
        // Arrange
        var configuration = new GoogleCalendarSettingsConfiguration();
        var options = new GoogleCalendarSettings
        {
            Enabled = true,
            CalendarId = " "
        };

        // Act
        var result = configuration.Validate(Options.DefaultName, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Be("GoogleCalendar:CalendarId must be configured.");
    }

    [Fact(DisplayName = "Validate should throw when options is null.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldThrowWhenOptionsIsNull()
    {
        // Arrange
        var configuration = new GoogleCalendarSettingsConfiguration();

        // Act
        var action = () => configuration.Validate(Options.DefaultName, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Validate should succeed when calendar id is configured.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldSucceedWhenCalendarIdIsConfigured()
    {
        // Arrange
        var configuration = new GoogleCalendarSettingsConfiguration();
        var options = new GoogleCalendarSettings
        {
            Enabled = true,
            CalendarId = "primary"
        };

        // Act
        var result = configuration.Validate(Options.DefaultName, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }
}

