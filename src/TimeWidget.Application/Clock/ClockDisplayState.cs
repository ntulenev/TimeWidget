namespace TimeWidget.Application.Clock;

/// <summary>
/// Represents the clock section state shown in the widget.
/// </summary>
/// <param name="TimeText">The local time text.</param>
/// <param name="DateText">The formatted date text.</param>
/// <param name="LeftCityTimes">The clocks displayed on the left side.</param>
/// <param name="RightCityTimes">The clocks displayed on the right side.</param>
public sealed record ClockDisplayState(
    string TimeText,
    string DateText,
    IReadOnlyList<CityClockDisplayState> LeftCityTimes,
    IReadOnlyList<CityClockDisplayState> RightCityTimes);
