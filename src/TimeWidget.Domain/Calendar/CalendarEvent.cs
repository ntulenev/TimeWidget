namespace TimeWidget.Domain.Calendar;

public sealed record CalendarEvent(
    string Title,
    DateTimeOffset Start,
    DateTimeOffset? End,
    bool IsAllDay,
    string? SelfResponseStatus)
{
    public string SafeTitle => string.IsNullOrWhiteSpace(Title) ? "Untitled event" : Title;
}
