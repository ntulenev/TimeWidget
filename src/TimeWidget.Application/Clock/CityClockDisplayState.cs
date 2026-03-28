namespace TimeWidget.Application.Clock;

/// <summary>
/// Represents a single city clock entry in the widget.
/// </summary>
/// <param name="Name">The display name of the city.</param>
/// <param name="TimeText">The formatted time text.</param>
public sealed record CityClockDisplayState(string Name, string TimeText);
