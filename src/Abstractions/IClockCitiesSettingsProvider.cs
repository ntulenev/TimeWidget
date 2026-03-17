using TimeWidget.Models;

namespace TimeWidget.Abstractions;

public interface IClockCitiesSettingsProvider
{
    ClockCitiesSettings Load();
}
