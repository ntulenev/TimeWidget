namespace TimeWidget.ViewModels;

public sealed class CalendarEventItemViewModel
{
    public CalendarEventItemViewModel(
        string title,
        string scheduleText,
        string responseSymbol)
    {
        Title = title;
        ScheduleText = scheduleText;
        ResponseSymbol = responseSymbol;
    }

    public string Title { get; }

    public string ScheduleText { get; }

    public string ResponseSymbol { get; }
}
