namespace TimeWidget.Models;

public sealed class CalendarAgendaResult
{
    public IReadOnlyList<CalendarEventInfo> Events { get; init; } = [];

    public string StatusMessage { get; init; } = string.Empty;
}
