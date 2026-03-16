using System.IO;
using System.Text.Json;

using TimeWidget.Abstractions;
using TimeWidget.Models;

namespace TimeWidget.Infrastructure;

public sealed class JsonWidgetSettingsStore : IWidgetSettingsStore
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TimeWidget",
        "widget-settings.json");

    public void SaveWindowPlacement(WidgetPlacementSettings placement)
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

    public bool TryLoadWindowPlacement(out WidgetPlacementSettings placement)
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                placement = default;
                return false;
            }

            var payload = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<WidgetPlacementSettings>(payload);
            if (double.IsNaN(settings.Left) || double.IsNaN(settings.Top))
            {
                placement = default;
                return false;
            }

            placement = settings;
            return true;
        }
        catch
        {
            placement = default;
            return false;
        }
    }
}

