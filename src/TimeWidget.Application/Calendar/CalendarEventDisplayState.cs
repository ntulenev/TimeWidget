namespace TimeWidget.Application.Calendar;

/// <summary>
/// Represents a single calendar event in the widget.
/// </summary>
/// <param name="Title">The event title.</param>
/// <param name="ScheduleText">The formatted schedule text.</param>
/// <param name="ResponseSymbol">The short response marker.</param>
public sealed record CalendarEventDisplayState(
    string Title,
    string ScheduleText,
    string ResponseSymbol);
