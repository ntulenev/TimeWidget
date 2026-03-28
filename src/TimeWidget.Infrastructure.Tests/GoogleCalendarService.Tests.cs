using FluentAssertions;

using Microsoft.Extensions.Options;

using TimeWidget.Application.Calendar;
using TimeWidget.Domain.Configuration;
using TimeWidget.Infrastructure.Calendar;

namespace TimeWidget.Infrastructure.Tests;

public sealed class GoogleCalendarServiceTests
{
    [Fact(DisplayName = "Constructor should throw when settings is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenSettingsIsNull()
    {
        // Arrange
        var action = () => new GoogleCalendarService(null!);

        // Act
        
        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Is Enabled should use configured flag.")]
    [Trait("Category", "Unit")]
    public void IsEnabledShouldUseConfiguredFlag()
    {
        // Arrange
        var enabledService = new GoogleCalendarService(Options.Create(new GoogleCalendarSettings
        {
            Enabled = true
        }));
        var disabledService = new GoogleCalendarService(Options.Create(new GoogleCalendarSettings
        {
            Enabled = false
        }));

        // Act
        var enabled = enabledService.IsEnabled;
        var disabled = disabledService.IsEnabled;

        // Assert
        enabled.Should().BeTrue();
        disabled.Should().BeFalse();
    }

    [Fact(DisplayName = "Get Upcoming Events should return disabled when calendar is off.")]
    [Trait("Category", "Unit")]
    public async Task GetUpcomingEventsAsyncShouldReturnDisabledWhenCalendarIsOff()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var service = new GoogleCalendarService(Options.Create(new GoogleCalendarSettings
        {
            Enabled = false
        }));

        // Act
        var result = await service.GetUpcomingEventsAsync(CalendarInteractionMode.Background, cts.Token);

        // Assert
        result.Status.Should().Be(CalendarLoadStatus.Disabled);
        result.Agenda.Events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Get Upcoming Events should return client secrets missing when file does not exist.")]
    [Trait("Category", "Unit")]
    public async Task GetUpcomingEventsAsyncShouldReturnClientSecretsMissingWhenFileDoesNotExist()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var service = new GoogleCalendarService(Options.Create(new GoogleCalendarSettings
        {
            ClientSecretsPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json")
        }));

        // Act
        var result = await service.GetUpcomingEventsAsync(CalendarInteractionMode.Background, cts.Token);

        // Assert
        result.Status.Should().Be(CalendarLoadStatus.ClientSecretsMissing);
    }

    [Fact(DisplayName = "Get Upcoming Events should require authorization when token is missing.")]
    [Trait("Category", "Unit")]
    public async Task GetUpcomingEventsAsyncShouldRequireAuthorizationWhenTokenIsMissing()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var tempDirectory = Directory.CreateTempSubdirectory();

        try
        {
            // Arrange
            var clientSecretsPath = Path.Combine(tempDirectory.FullName, "client.json");
            await File.WriteAllTextAsync(
                clientSecretsPath,
                """
                {
                  "installed": {
                    "client_id": "timewidget-test.apps.googleusercontent.com",
                    "project_id": "timewidget-test",
                    "auth_uri": "https://accounts.google.com/o/oauth2/auth",
                    "token_uri": "https://oauth2.googleapis.com/token",
                    "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
                    "client_secret": "secret",
                    "redirect_uris": [ "http://localhost" ]
                  }
                }
                """);
            var service = new GoogleCalendarService(Options.Create(new GoogleCalendarSettings
            {
                ClientSecretsPath = clientSecretsPath,
                TokenStoreDirectory = Path.Combine(tempDirectory.FullName, "tokens")
            }));

            // Act
            var result = await service.GetUpcomingEventsAsync(
                CalendarInteractionMode.Background,
                cts.Token);

            // Assert
            result.Status.Should().Be(CalendarLoadStatus.AuthorizationRequired);
            result.Agenda.Events.Should().BeEmpty();
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact(DisplayName = "Forget Authorization should delete token store directory when secrets are missing.")]
    [Trait("Category", "Unit")]
    public async Task ForgetAuthorizationAsyncShouldDeleteTokenStoreDirectoryWhenSecretsAreMissing()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var tokenStoreDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tokenStoreDirectory);
        File.WriteAllText(Path.Combine(tokenStoreDirectory, "token.json"), "{}");
        var service = new GoogleCalendarService(Options.Create(new GoogleCalendarSettings
        {
            ClientSecretsPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json"),
            TokenStoreDirectory = tokenStoreDirectory
        }));

        // Act
        await service.ForgetAuthorizationAsync(cts.Token);

        // Assert
        Directory.Exists(tokenStoreDirectory).Should().BeFalse();
    }

    [Fact(DisplayName = "Forget Authorization should do nothing when calendar is disabled.")]
    [Trait("Category", "Unit")]
    public async Task ForgetAuthorizationAsyncShouldDoNothingWhenCalendarIsDisabled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var tokenStoreDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tokenStoreDirectory);

        try
        {
            // Arrange
            var service = new GoogleCalendarService(Options.Create(new GoogleCalendarSettings
            {
                Enabled = false,
                TokenStoreDirectory = tokenStoreDirectory
            }));

            // Act
            await service.ForgetAuthorizationAsync(cts.Token);

            // Assert
            Directory.Exists(tokenStoreDirectory).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tokenStoreDirectory, recursive: true);
        }
    }
}


