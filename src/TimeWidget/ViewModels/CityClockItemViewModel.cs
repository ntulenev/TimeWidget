using TimeWidget.Application.Clock;

namespace TimeWidget.ViewModels;

/// <summary>
/// Represents a city clock row in the UI.
/// </summary>
public sealed class CityClockItemViewModel : ObservableObject
{
    /// <summary>
    /// Gets the city name.
    /// </summary>
    public string Name {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Gets the formatted time text.
    /// </summary>
    public string TimeText {
        get;
        private set => SetProperty(ref field, value);
    } = "--:--";

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
