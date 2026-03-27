namespace TimeWidget.Application.Calendar;

public sealed record CalendarDisplayState(
    IReadOnlyList<CalendarEventDisplayState> Events,
    string StatusText,
    bool ShowSection,
    bool ShowEvents,
    bool ShowStatus,
    bool ShowCompactSection,
    bool ShowFullSection);
