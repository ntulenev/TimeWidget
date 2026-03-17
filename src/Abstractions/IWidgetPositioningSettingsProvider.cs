using TimeWidget.Models;

namespace TimeWidget.Abstractions;

public interface IWidgetPositioningSettingsProvider
{
    WidgetPositioningSettings Load();
}
