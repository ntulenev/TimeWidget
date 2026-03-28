using System.Globalization;

using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using Microsoft.Extensions.Options;

using TimeWidget.Application.Abstractions;
using TimeWidget.Application.Calendar;
using TimeWidget.Domain.Calendar;
using TimeWidget.Domain.Configuration;
using TimeWidget.Infrastructure.Configuration;

namespace TimeWidget.Infrastructure.Calendar;

/// <summary>
/// Loads Google Calendar events for the widget.
/// </summary>
public sealed class GoogleCalendarService : ICalendarService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCalendarService"/> class.
    /// </summary>
    /// <param name="settings">The configured Google Calendar settings.</param>
    public GoogleCalendarService(IOptions<GoogleCalendarSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings.Value;
    }

    /// <inheritdoc />
    public bool IsEnabled => _settings.Enabled;

    /// <inheritdoc />
    public async Task<CalendarLoadResult> GetUpcomingEventsAsync(
        CalendarInteractionMode interactionMode,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.Disabled);
        }

        var clientSecretsPath = PathResolver.ResolvePath(_settings.ClientSecretsPath);
        if (string.IsNullOrWhiteSpace(clientSecretsPath) || !File.Exists(clientSecretsPath))
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.ClientSecretsMissing);
        }

        try
        {
            using var stream = File.OpenRead(clientSecretsPath);
            var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
            var flow = CreateAuthorizationFlow(clientSecrets);
            var credential = await GetCredentialAsync(flow, interactionMode, cancellationToken);
            if (credential is null)
            {
                return CalendarLoadResult.FromStatus(CalendarLoadStatus.AuthorizationRequired);
            }

            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "TimeWidget"
            });

            var maxEvents = _settings.ActiveMaxEvents;
            var request = service.Events.List(_settings.CalendarId);
            request.MaxResults = Math.Min(50, Math.Max(MIN_CANDIDATE_EVENTS_TO_FETCH, maxEvents * 8));
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
                .Cast<CalendarEvent>()
                .Take(maxEvents)
                .ToArray()
                ?? [];

            return agendaItems.Length > 0
                ? CalendarLoadResult.Success(new CalendarAgenda(agendaItems))
                : CalendarLoadResult.FromStatus(CalendarLoadStatus.NoUpcomingEvents);
        }
        catch (TokenResponseException)
        {
            await ForgetAuthorizationAsync(cancellationToken);
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.AuthorizationRequired);
        }
        catch (GoogleApiException)
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.AccessDenied);
        }
        catch
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.Unavailable);
        }
    }

    /// <inheritdoc />
    public async Task ForgetAuthorizationAsync(CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            return;
        }

        var clientSecretsPath = PathResolver.ResolvePath(_settings.ClientSecretsPath);
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
            var token = await flow.LoadTokenAsync(TOKEN_STORE_USER_ID, cancellationToken);
            if (!string.IsNullOrWhiteSpace(token?.RefreshToken))
            {
                await flow.RevokeTokenAsync(TOKEN_STORE_USER_ID, token.RefreshToken, cancellationToken);
            }

            await flow.DeleteTokenAsync(TOKEN_STORE_USER_ID, cancellationToken);
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
            Scopes = _scopes,
            LoginHint = string.IsNullOrWhiteSpace(_settings.LoginHint) ? null : _settings.LoginHint,
            Prompt = _settings.ForceAccountSelection ? "select_account" : null
        };

        return new PkceGoogleAuthorizationCodeFlow(initializer);
    }

    private static async Task<UserCredential?> GetCredentialAsync(
        GoogleAuthorizationCodeFlow flow,
        CalendarInteractionMode interactionMode,
        CancellationToken cancellationToken)
    {
        if (interactionMode == CalendarInteractionMode.Interactive)
        {
            var authApp = new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver());
            return await authApp.AuthorizeAsync(TOKEN_STORE_USER_ID, cancellationToken);
        }

        var token = await flow.LoadTokenAsync(TOKEN_STORE_USER_ID, cancellationToken);
        return token is null
            ? null
            : new UserCredential(flow, TOKEN_STORE_USER_ID, token);
    }

    private string GetTokenStoreDirectory()
    {
        var configuredPath = PathResolver.ResolvePath(_settings.TokenStoreDirectory);
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
        var tokenStoreDirectory = PathResolver.ResolvePath(_settings.TokenStoreDirectory);
        if (string.IsNullOrWhiteSpace(tokenStoreDirectory) || !Directory.Exists(tokenStoreDirectory))
        {
            return;
        }

        Directory.Delete(tokenStoreDirectory, recursive: true);
    }

    private static CalendarEvent? MapEvent(Event calendarEvent)
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

        return new CalendarEvent(
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

    private const string TOKEN_STORE_USER_ID = "timewidget-google-calendar";
    private const int MIN_CANDIDATE_EVENTS_TO_FETCH = 20;
    private static readonly string[] _scopes =
    [
        CalendarService.Scope.CalendarEventsReadonly
    ];

    private readonly GoogleCalendarSettings _settings;
}
