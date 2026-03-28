using System.Globalization;

using TimeWidget.Domain.Calendar;
using TimeWidget.Domain.Configuration;

namespace TimeWidget.Application.Calendar;

/// <summary>
/// Converts calendar domain results into UI-facing display state.
/// </summary>
public sealed class CalendarDisplayBuilder
{
    private readonly CultureInfo _dateCulture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>
    /// Builds the calendar display model for the main widget view.
    /// </summary>
    /// <param name="result">The loaded calendar result.</param>
    /// <param name="settings">The calendar settings that control the layout.</param>
    /// <param name="now">The current timestamp used to label relative dates.</param>
    /// <returns>The display state to render.</returns>
    public CalendarDisplayState Build(
        CalendarLoadResult result,
        GoogleCalendarSettings settings,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(settings);

        var events = result.Agenda.Events
            .Select(calendarEvent => CreateCalendarEvent(calendarEvent, now))
            .ToArray();
        var statusText = GetStatusText(result.Status);
        var showSection = events.Length > 0 || !string.IsNullOrWhiteSpace(statusText);

        return new CalendarDisplayState(
            events,
            statusText,
            showSection,
            events.Length > 0,
            !string.IsNullOrWhiteSpace(statusText),
            showSection && !settings.IsFullCalendarMode,
            showSection && settings.IsFullCalendarMode);
    }

    /// <summary>
    /// Builds the display state shown after calendar authorization has been removed.
    /// </summary>
    /// <param name="settings">The calendar settings that control the layout.</param>
    /// <returns>The signed-out display state.</returns>
    public CalendarDisplayState BuildSignedOutState(GoogleCalendarSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return Build(
            CalendarLoadResult.FromStatus(CalendarLoadStatus.AuthorizationRemoved),
            settings,
            DateTimeOffset.Now);
    }

    private CalendarEventDisplayState CreateCalendarEvent(
        CalendarEvent calendarEvent,
        DateTimeOffset now)
    {
        var localStart = calendarEvent.Start.ToLocalTime();
        var dayLabel = GetCalendarDayLabel(localStart.Date, now.Date);
        var scheduleText = calendarEvent.IsAllDay
            ? $"{dayLabel} - All day"
            : $"{dayLabel} - {localStart:HH:mm}";

        return new CalendarEventDisplayState(
            calendarEvent.SafeTitle,
            scheduleText,
            GetResponseSymbol(calendarEvent.SelfResponseStatus));
    }

    private static string GetStatusText(CalendarLoadStatus status)
    {
        return status switch
        {
            CalendarLoadStatus.Success => string.Empty,
            CalendarLoadStatus.Disabled => string.Empty,
            CalendarLoadStatus.ClientSecretsMissing => "Add Google OAuth client JSON to connect calendar",
            CalendarLoadStatus.AuthorizationRequired => "Use tray -> Refresh calendar to connect Google Calendar",
            CalendarLoadStatus.AuthorizationRemoved => "Google Calendar sign-in removed",
            CalendarLoadStatus.AccessDenied => "Calendar not found or access denied",
            CalendarLoadStatus.Unavailable => "Google Calendar unavailable",
            CalendarLoadStatus.NoUpcomingEvents => "No upcoming meetings",
            _ => string.Empty
        };
    }

    private static string GetResponseSymbol(string? selfResponseStatus)
    {
        if (string.Equals(selfResponseStatus, "accepted", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (string.Equals(selfResponseStatus, "tentative", StringComparison.OrdinalIgnoreCase))
        {
            return "M";
        }

        if (string.Equals(selfResponseStatus, "declined", StringComparison.OrdinalIgnoreCase))
        {
            return "N";
        }

        return "?";
    }

    private string GetCalendarDayLabel(DateTime eventDate, DateTime currentDate)
    {
        if (eventDate == currentDate)
        {
            return "Today";
        }

        if (eventDate == currentDate.AddDays(1))
        {
            return "Tomorrow";
        }

        return eventDate.ToString("ddd, d MMM", _dateCulture);
    }
}
