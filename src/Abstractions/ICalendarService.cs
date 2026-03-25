using TimeWidget.Models;

namespace TimeWidget.Abstractions;

public interface ICalendarService
{
    bool IsEnabled { get; }

    Task<CalendarAgendaResult> GetUpcomingEventsAsync(
        bool interactive,
        CancellationToken cancellationToken);

    Task ForgetAuthorizationAsync(CancellationToken cancellationToken);
}
