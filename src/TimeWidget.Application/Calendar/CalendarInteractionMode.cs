namespace TimeWidget.Application.Calendar;

/// <summary>
/// Describes how a calendar refresh was triggered.
/// </summary>
public enum CalendarInteractionMode
{
    /// <summary>
    /// The refresh runs automatically in the background.
    /// </summary>
    Background = 0,

    /// <summary>
    /// The refresh was triggered by a direct user action.
    /// </summary>
    Interactive = 1
}
