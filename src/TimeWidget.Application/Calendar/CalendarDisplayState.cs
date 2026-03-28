namespace TimeWidget.Application.Calendar;

/// <summary>
/// Represents the calendar section state shown in the widget.
/// </summary>
/// <param name="Events">The events to display.</param>
/// <param name="StatusText">The status message shown when no events are available or an action is required.</param>
/// <param name="ShowSection">A value indicating whether the calendar section should be visible.</param>
/// <param name="ShowEvents">A value indicating whether event items should be visible.</param>
/// <param name="ShowStatus">A value indicating whether the status text should be visible.</param>
/// <param name="ShowCompactSection">A value indicating whether the compact calendar layout should be shown.</param>
/// <param name="ShowFullSection">A value indicating whether the full calendar layout should be shown.</param>
public sealed record CalendarDisplayState(
    IReadOnlyList<CalendarEventDisplayState> Events,
    string StatusText,
    bool ShowSection,
    bool ShowEvents,
    bool ShowStatus,
    bool ShowCompactSection,
    bool ShowFullSection);
