using TimeWidget.Domain.Calendar;

namespace TimeWidget.Application.Calendar;

public sealed class CalendarLoadResult
{
    private CalendarLoadResult(CalendarLoadStatus status, CalendarAgenda agenda)
    {
        Status = status;
        Agenda = agenda;
    }

    public CalendarLoadStatus Status { get; }

    public CalendarAgenda Agenda { get; }

    public static CalendarLoadResult Success(CalendarAgenda agenda) =>
        new(CalendarLoadStatus.Success, agenda);

    public static CalendarLoadResult FromStatus(CalendarLoadStatus status) =>
        new(status, CalendarAgenda.Empty);
}
