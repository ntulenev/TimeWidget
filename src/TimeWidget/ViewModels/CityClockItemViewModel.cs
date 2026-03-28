using TimeWidget.Application.Clock;

namespace TimeWidget.ViewModels;

/// <summary>
/// Represents a city clock row in the UI.
/// </summary>
public sealed class CityClockItemViewModel : ObservableObject
{
    private string _name = string.Empty;
    private string _timeText = "--:--";

    /// <summary>
    /// Gets the city name.
    /// </summary>
    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    /// <summary>
    /// Gets the formatted time text.
    /// </summary>
    public string TimeText
    {
        get => _timeText;
        private set => SetProperty(ref _timeText, value);
    }

    /// <summary>
    /// Applies the provided display state to the view model.
    /// </summary>
    /// <param name="state">The state to apply.</param>
    public void Apply(CityClockDisplayState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        Name = state.Name;
        TimeText = state.TimeText;
    }
}
