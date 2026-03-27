namespace TimeWidget.Domain.Calendar;

public sealed class CalendarAgenda
{
    public static CalendarAgenda Empty { get; } = new([]);

    public CalendarAgenda(IReadOnlyList<CalendarEvent> events)
    {
        Events = events ?? [];
    }

    public IReadOnlyList<CalendarEvent> Events { get; }
}
