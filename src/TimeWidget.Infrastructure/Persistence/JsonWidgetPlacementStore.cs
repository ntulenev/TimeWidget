using System.IO;
using System.Text.Json;

using TimeWidget.Application.Abstractions;
using TimeWidget.Domain.Widget;

namespace TimeWidget.Infrastructure.Persistence;

public sealed class JsonWidgetPlacementStore : IWidgetPlacementStore
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TimeWidget",
        "widget-settings.json");

    public void SaveWindowPlacement(WidgetPlacement placement)
    {
        try
        {
            var settingsDirectory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrWhiteSpace(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
            }

            var payload = JsonSerializer.Serialize(
                placement,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(SettingsFilePath, payload);
        }
        catch
        {
            // Persisting position is best-effort only. The widget should still work if writing fails.
        }
    }

    public bool TryLoadWindowPlacement(out WidgetPlacement placement)
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                placement = default;
                return false;
            }

            var payload = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<WidgetPlacement>(payload);
            if (double.IsNaN(settings.Left) || double.IsNaN(settings.Top))
            {
                placement = default;
                return false;
            }

            placement = settings.WithUnit(string.IsNullOrWhiteSpace(settings.Unit)
                ? WidgetPlacement.PixelUnit
                : settings.Unit);
            return true;
        }
        catch
        {
            placement = default;
            return false;
        }
    }
}

