namespace TimeWidget.Models;

public sealed record CalendarEventInfo(
    string Title,
    DateTimeOffset Start,
    DateTimeOffset? End,
    bool IsAllDay,
    string? SelfResponseStatus);
