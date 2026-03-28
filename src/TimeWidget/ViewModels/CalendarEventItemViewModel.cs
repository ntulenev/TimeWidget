namespace TimeWidget.ViewModels;

/// <summary>
/// Represents a calendar event row in the UI.
/// </summary>
public sealed class CalendarEventItemViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarEventItemViewModel"/> class.
    /// </summary>
    /// <param name="title">The event title.</param>
    /// <param name="scheduleText">The formatted schedule text.</param>
    /// <param name="responseSymbol">The attendee response symbol.</param>
    public CalendarEventItemViewModel(
        string title,
        string scheduleText,
        string responseSymbol)
    {
        Title = title;
        ScheduleText = scheduleText;
        ResponseSymbol = responseSymbol;
    }

    /// <summary>
    /// Gets the event title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the formatted schedule text.
    /// </summary>
    public string ScheduleText { get; }

    /// <summary>
    /// Gets the attendee response symbol.
    /// </summary>
    public string ResponseSymbol { get; }
}
