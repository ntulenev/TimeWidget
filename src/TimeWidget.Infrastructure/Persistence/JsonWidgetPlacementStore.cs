using System.Text.Json;

using TimeWidget.Application.Abstractions;
using TimeWidget.Domain.Widget;

namespace TimeWidget.Infrastructure.Persistence;

/// <summary>
/// Persists widget placement to a JSON file in local application data.
/// </summary>
public sealed class JsonWidgetPlacementStore : IWidgetPlacementStore
{
    /// <inheritdoc />
    public void SaveWindowPlacement(WidgetPlacement placement)
    {
        try
        {
            var settingsDirectory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrWhiteSpace(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
            }

            var payload = JsonSerializer.Serialize(placement, _serializerOptions);

            File.WriteAllText(_settingsFilePath, payload);
        }
        catch
        {
            // Persisting position is best-effort only. The widget should still work if writing fails.
        }
    }

    /// <inheritdoc />
    public bool TryLoadWindowPlacement(out WidgetPlacement placement)
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                placement = default;
                return false;
            }

            var payload = File.ReadAllText(_settingsFilePath);
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

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string _settingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TimeWidget",
        "widget-settings.json");
}

