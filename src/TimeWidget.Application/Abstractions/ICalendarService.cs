using TimeWidget.Application.Calendar;

namespace TimeWidget.Application.Abstractions;

public interface ICalendarService
{
    bool IsEnabled { get; }

    Task<CalendarLoadResult> GetUpcomingEventsAsync(
        CalendarInteractionMode interactionMode,
        CancellationToken cancellationToken);

    Task ForgetAuthorizationAsync(CancellationToken cancellationToken);
}
