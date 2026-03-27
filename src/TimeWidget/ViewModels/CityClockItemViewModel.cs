using TimeWidget.Application.Clock;

namespace TimeWidget.ViewModels;

public sealed class CityClockItemViewModel : ObservableObject
{
    private string _name = string.Empty;
    private string _timeText = "--:--";

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    public string TimeText
    {
        get => _timeText;
        private set => SetProperty(ref _timeText, value);
    }

    public void Apply(CityClockDisplayState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        Name = state.Name;
        TimeText = state.TimeText;
    }
}
