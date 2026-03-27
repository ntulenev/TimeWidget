using TimeWidget.Domain.Widget;

namespace TimeWidget.Application.Abstractions;

public interface IWidgetPlacementStore
{
    void SaveWindowPlacement(WidgetPlacement placement);

    bool TryLoadWindowPlacement(out WidgetPlacement placement);
}
