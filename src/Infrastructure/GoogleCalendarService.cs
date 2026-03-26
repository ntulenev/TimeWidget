using System.Globalization;
using System.IO;

using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using Microsoft.Extensions.Options;

using TimeWidget.Abstractions;
using TimeWidget.Models;

namespace TimeWidget.Infrastructure;

public sealed class GoogleCalendarService : ICalendarService
{
    private const string TokenStoreUserId = "timewidget-google-calendar";
    private const int MinCandidateEventsToFetch = 20;
    private static readonly string[] Scopes =
    [
        CalendarService.Scope.CalendarEventsReadonly
    ];

    private readonly GoogleCalendarSettings _settings;

    public GoogleCalendarService(IOptions<GoogleCalendarSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings.Value;
    }

    public bool IsEnabled => _settings.Enabled;

    public async Task<CalendarAgendaResult> GetUpcomingEventsAsync(
        bool interactive,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            return new CalendarAgendaResult();
        }

        var clientSecretsPath = ResolvePath(_settings.ClientSecretsPath);
        if (string.IsNullOrWhiteSpace(clientSecretsPath) || !File.Exists(clientSecretsPath))
        {
            return new CalendarAgendaResult
            {
                StatusMessage = "Add Google OAuth client JSON to connect calendar"
            };
        }

        try
        {
            using var stream = File.OpenRead(clientSecretsPath);
            var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
            var flow = CreateAuthorizationFlow(clientSecrets);
            var credential = await GetCredentialAsync(flow, interactive, cancellationToken);
            if (credential is null)
            {
                return new CalendarAgendaResult
                {
                    StatusMessage = "Use tray -> Refresh calendar to connect Google Calendar"
                };
            }

            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "TimeWidget"
            });

            var maxEvents = _settings.ActiveMaxEvents;
            var request = service.Events.List(_settings.CalendarId);
            request.MaxResults = Math.Min(
                50,
                Math.Max(MinCandidateEventsToFetch, maxEvents * 8));
            request.EventTypes = EventsResource.ListRequest.EventTypesEnum.Default__;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.TimeMinDateTimeOffset = DateTimeOffset.UtcNow;
            request.Fields =
                "items(summary,start,end,eventType,attendees(self,responseStatus),organizer(self))";

            var events = await request.ExecuteAsync(cancellationToken);
            var agendaItems = events.Items?
                .Select(MapEvent)
                .Where(item => item is not null)
                .Cast<CalendarEventInfo>()
                .Take(maxEvents)
                .ToArray()
                ?? [];

            return agendaItems.Length > 0
                ? new CalendarAgendaResult
                {
                    Events = agendaItems
                }
                : new CalendarAgendaResult
                {
                    StatusMessage = "No upcoming meetings"
                };
        }
        catch (TokenResponseException)
        {
            await ForgetAuthorizationAsync(cancellationToken);
            return new CalendarAgendaResult
            {
                StatusMessage = "Refresh calendar to reconnect Google Calendar"
            };
        }
        catch (GoogleApiException)
        {
            return new CalendarAgendaResult
            {
                StatusMessage = "Calendar not found or access denied"
            };
        }
        catch
        {
            return new CalendarAgendaResult
            {
                StatusMessage = "Google Calendar unavailable"
            };
        }
    }

    public async Task ForgetAuthorizationAsync(CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            return;
        }

        var clientSecretsPath = ResolvePath(_settings.ClientSecretsPath);
        if (string.IsNullOrWhiteSpace(clientSecretsPath) || !File.Exists(clientSecretsPath))
        {
            DeleteTokenStoreDirectory();
            return;
        }

        try
        {
            using var stream = File.OpenRead(clientSecretsPath);
            var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
            var flow = CreateAuthorizationFlow(clientSecrets);
            var token = await flow.LoadTokenAsync(TokenStoreUserId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(token?.RefreshToken))
            {
                await flow.RevokeTokenAsync(TokenStoreUserId, token.RefreshToken, cancellationToken);
            }

            await flow.DeleteTokenAsync(TokenStoreUserId, cancellationToken);
        }
        catch
        {
            DeleteTokenStoreDirectory();
        }
    }

    private GoogleAuthorizationCodeFlow CreateAuthorizationFlow(ClientSecrets clientSecrets)
    {
        var initializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = clientSecrets,
            DataStore = new FileDataStore(GetTokenStoreDirectory(), true),
            Scopes = Scopes,
            LoginHint = string.IsNullOrWhiteSpace(_settings.LoginHint) ? null : _settings.LoginHint,
            Prompt = _settings.ForceAccountSelection ? "select_account" : null
        };

        return new PkceGoogleAuthorizationCodeFlow(initializer);
    }

    private async Task<UserCredential?> GetCredentialAsync(
        GoogleAuthorizationCodeFlow flow,
        bool interactive,
        CancellationToken cancellationToken)
    {
        if (interactive)
        {
            var authApp = new AuthorizationCodeInstalledApp(
                flow,
                new LocalServerCodeReceiver());
            return await authApp.AuthorizeAsync(TokenStoreUserId, cancellationToken);
        }

        var token = await flow.LoadTokenAsync(TokenStoreUserId, cancellationToken);
        return token is null
            ? null
            : new UserCredential(flow, TokenStoreUserId, token);
    }

    private string GetTokenStoreDirectory()
    {
        var configuredPath = ResolvePath(_settings.TokenStoreDirectory);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            Directory.CreateDirectory(configuredPath);
            return configuredPath;
        }

        var fallbackPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TimeWidget",
            "GoogleCalendarToken");
        Directory.CreateDirectory(fallbackPath);
        return fallbackPath;
    }

    private void DeleteTokenStoreDirectory()
    {
        var tokenStoreDirectory = ResolvePath(_settings.TokenStoreDirectory);
        if (string.IsNullOrWhiteSpace(tokenStoreDirectory) || !Directory.Exists(tokenStoreDirectory))
        {
            return;
        }

        Directory.Delete(tokenStoreDirectory, recursive: true);
    }

    private static CalendarEventInfo? MapEvent(Event calendarEvent)
    {
        if (!IsMeeting(calendarEvent) || calendarEvent.Start is null)
        {
            return null;
        }

        var start = ResolveEventDateTime(calendarEvent.Start);
        if (start is null)
        {
            return null;
        }

        var isAllDay = !string.IsNullOrWhiteSpace(calendarEvent.Start.Date);
        var end = ResolveEventDateTime(calendarEvent.End);
        var selfAttendee = calendarEvent.Attendees?.FirstOrDefault(attendee => attendee.Self is true);
        var selfResponseStatus = selfAttendee?.ResponseStatus;
        if (string.IsNullOrWhiteSpace(selfResponseStatus) && calendarEvent.Organizer?.Self is true)
        {
            selfResponseStatus = "accepted";
        }

        return new CalendarEventInfo(
            string.IsNullOrWhiteSpace(calendarEvent.Summary) ? "Untitled event" : calendarEvent.Summary,
            start.Value,
            end,
            isAllDay,
            selfResponseStatus);
    }

    private static bool IsMeeting(Event calendarEvent)
    {
        if (!string.Equals(calendarEvent.EventType, "default", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(calendarEvent.Start?.Date))
        {
            return false;
        }

        if (calendarEvent.Attendees is null || calendarEvent.Attendees.Count == 0)
        {
            return false;
        }

        return calendarEvent.Attendees.Any(attendee => attendee.Self is not true);
    }

    private static DateTimeOffset? ResolveEventDateTime(EventDateTime? eventDateTime)
    {
        if (eventDateTime?.DateTimeDateTimeOffset is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset;
        }

        if (!string.IsNullOrWhiteSpace(eventDateTime?.DateTimeRaw) &&
            DateTimeOffset.TryParse(
                eventDateTime.DateTimeRaw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsedDateTimeOffset))
        {
            return parsedDateTimeOffset;
        }

        if (string.IsNullOrWhiteSpace(eventDateTime?.Date))
        {
            return null;
        }

        if (!DateOnly.TryParseExact(
                eventDateTime.Date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            return null;
        }

        var localDateTime = parsedDate.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime));
    }

    private static string ResolvePath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        var expandedPath = Environment.ExpandEnvironmentVariables(configuredPath.Trim());
        if (Path.IsPathRooted(expandedPath))
        {
            return expandedPath;
        }

        var currentDirectoryPath = Path.GetFullPath(expandedPath, Environment.CurrentDirectory);
        if (File.Exists(currentDirectoryPath) || Directory.Exists(currentDirectoryPath))
        {
            return currentDirectoryPath;
        }

        return Path.GetFullPath(expandedPath, AppContext.BaseDirectory);
    }
}
