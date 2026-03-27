namespace TimeWidget.Application.Clock;

public sealed record ClockDisplayState(
    string TimeText,
    string DateText,
    IReadOnlyList<CityClockDisplayState> LeftCityTimes,
    IReadOnlyList<CityClockDisplayState> RightCityTimes);
