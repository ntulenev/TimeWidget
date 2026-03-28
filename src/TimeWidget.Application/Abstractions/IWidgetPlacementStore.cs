using TimeWidget.Domain.Widget;

namespace TimeWidget.Application.Abstractions;

/// <summary>
/// Persists and restores widget placement.
/// </summary>
public interface IWidgetPlacementStore
{
    /// <summary>
    /// Saves the current widget placement.
    /// </summary>
    /// <param name="placement">The placement to persist.</param>
    void SaveWindowPlacement(WidgetPlacement placement);

    /// <summary>
    /// Attempts to load the previously saved widget placement.
    /// </summary>
    /// <param name="placement">When this method returns, contains the loaded placement if one was found.</param>
    /// <returns><see langword="true"/> when a saved placement was loaded; otherwise, <see langword="false"/>.</returns>
    bool TryLoadWindowPlacement(out WidgetPlacement placement);
}
