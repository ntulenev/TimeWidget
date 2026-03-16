using TimeWidget.Models;

namespace TimeWidget.Abstractions;

public interface IWidgetSettingsStore
{
    void SaveWindowPlacement(WidgetPlacementSettings placement);

    bool TryLoadWindowPlacement(out WidgetPlacementSettings placement);
}

