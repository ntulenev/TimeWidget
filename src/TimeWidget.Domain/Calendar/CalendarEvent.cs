namespace TimeWidget.Domain.Calendar;

/// <summary>
/// Represents a single calendar event.
/// </summary>
/// <param name="Title">The event title.</param>
/// <param name="Start">The event start time.</param>
/// <param name="End">The event end time, if available.</param>
/// <param name="IsAllDay">A value indicating whether the event lasts the whole day.</param>
/// <param name="SelfResponseStatus">The attendee response status for the current user.</param>
public sealed record CalendarEvent(
    string Title,
    DateTimeOffset Start,
    DateTimeOffset? End,
    bool IsAllDay,
    string? SelfResponseStatus)
{
    /// <summary>
    /// Gets a non-empty title suitable for display.
    /// </summary>
    public string SafeTitle => string.IsNullOrWhiteSpace(Title) ? "Untitled event" : Title;
}
