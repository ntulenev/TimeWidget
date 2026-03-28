namespace TimeWidget.Domain.Calendar;

/// <summary>
/// Represents a collection of upcoming calendar events.
/// </summary>
public sealed class CalendarAgenda
{
    /// <summary>
    /// Gets an empty agenda instance.
    /// </summary>
    public static CalendarAgenda Empty { get; } = new([]);

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarAgenda"/> class.
    /// </summary>
    /// <param name="events">The events contained in the agenda.</param>
    public CalendarAgenda(IReadOnlyList<CalendarEvent> events)
    {
        Events = events ?? [];
    }

    /// <summary>
    /// Gets the events contained in the agenda.
    /// </summary>
    public IReadOnlyList<CalendarEvent> Events { get; }
}
