using System.IO;
using System.Text.Json;

using TimeWidget.Abstractions;
using TimeWidget.Models;

namespace TimeWidget.Infrastructure;

public sealed class JsonAppSettingsWidgetPositioningSettingsProvider : IWidgetPositioningSettingsProvider
{
    private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public WidgetPositioningSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new WidgetPositioningSettings();
            }

            using var stream = File.OpenRead(SettingsFilePath);
            var payload = JsonSerializer.Deserialize<AppSettingsPayload>(stream, SerializerOptions);
            return Sanitize(payload?.WidgetPositioning);
        }
        catch
        {
            return new WidgetPositioningSettings();
        }
    }

    private static WidgetPositioningSettings Sanitize(WidgetPositioningSettings? settings)
    {
        var percent = settings?.CenterUpVerticalOffsetPercent ?? 15;
        percent = Math.Clamp(percent, 0, 100);

        return new WidgetPositioningSettings
        {
            CenterUpVerticalOffsetPercent = percent
        };
    }

    private sealed class AppSettingsPayload
    {
        public WidgetPositioningSettings? WidgetPositioning { get; init; }
    }
}
