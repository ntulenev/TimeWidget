using System.Globalization;

using TimeWidget.Domain.Calendar;
using TimeWidget.Domain.Configuration;

namespace TimeWidget.Application.Calendar;

public sealed class CalendarDisplayBuilder
{
    private static readonly CultureInfo DateCulture = CultureInfo.GetCultureInfo("en-US");

    public CalendarDisplayState Build(
        CalendarLoadResult result,
        GoogleCalendarSettings settings,
        DateTimeOffset now)
    {
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

    public CalendarDisplayState BuildSignedOutState(GoogleCalendarSettings settings)
    {
        return Build(
            CalendarLoadResult.FromStatus(CalendarLoadStatus.AuthorizationRemoved),
            settings,
            DateTimeOffset.Now);
    }

    private static CalendarEventDisplayState CreateCalendarEvent(
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

    private static string GetCalendarDayLabel(DateTime eventDate, DateTime currentDate)
    {
        if (eventDate == currentDate)
        {
            return "Today";
        }

        if (eventDate == currentDate.AddDays(1))
        {
            return "Tomorrow";
        }

        return eventDate.ToString("ddd, d MMM", DateCulture);
    }
}
