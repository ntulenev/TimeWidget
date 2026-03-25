using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TimeWidget.Abstractions;
using TimeWidget.Infrastructure;
using TimeWidget.Models;
using TimeWidget.ViewModels;
using TimeWidget.Views;

namespace TimeWidget;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTimeWidgetInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ClockCitiesSettings>()
            .Bind(configuration.GetSection("ClockCities"));
        services.PostConfigure<ClockCitiesSettings>(settings =>
        {
            settings.LeftCities = SanitizeCities(settings.LeftCities);
            settings.RightCities = SanitizeCities(settings.RightCities);
        });

        services.AddOptions<WidgetPositioningSettings>()
            .Bind(configuration.GetSection("WidgetPositioning"));
        services.PostConfigure<WidgetPositioningSettings>(settings =>
        {
            settings.CenterUpVerticalOffsetPercent = Math.Clamp(
                settings.CenterUpVerticalOffsetPercent,
                0,
                100);
            settings.Opacity = Math.Clamp(settings.Opacity, 0, 100);
            settings.ScalePercent = Math.Clamp(settings.ScalePercent, 50, 200);
        });

        services.AddOptions<GoogleCalendarSettings>()
            .Bind(configuration.GetSection("GoogleCalendar"));
        services.PostConfigure<GoogleCalendarSettings>(settings =>
        {
            settings.CalendarId = string.IsNullOrWhiteSpace(settings.CalendarId)
                ? "primary"
                : settings.CalendarId.Trim();
            settings.MaxEvents = Math.Clamp(settings.MaxEvents, 1, 8);
            settings.RefreshMinutes = Math.Clamp(settings.RefreshMinutes, 1, 60);
            settings.ClientSecretsPath = settings.ClientSecretsPath?.Trim() ?? string.Empty;
            settings.TokenStoreDirectory = settings.TokenStoreDirectory?.Trim() ?? string.Empty;
            settings.LoginHint = string.IsNullOrWhiteSpace(settings.LoginHint)
                ? null
                : settings.LoginHint.Trim();
        });

        services.AddSingleton<IClockService, SystemClockService>();
        services.AddSingleton<ICalendarService, GoogleCalendarService>();
        services.AddSingleton<ILocationService, WindowsLocationService>();
        services.AddSingleton<IWeatherService, OpenMeteoWeatherService>();
        services.AddSingleton<IWidgetSettingsStore, JsonWidgetSettingsStore>();

        return services;
    }

    public static IServiceCollection AddTimeWidgetPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<MainWindowViewModel>();
        services.AddScoped<MainWindow>();

        return services;
    }

    private static CityClockSettings[] SanitizeCities(CityClockSettings[]? cities)
    {
        if (cities is null || cities.Length == 0)
        {
            return [];
        }

        return cities
            .Where(city => !string.IsNullOrWhiteSpace(city.Name))
            .Select(city => new CityClockSettings
            {
                Name = city.Name.Trim(),
                TimeZoneId = city.TimeZoneId?.Trim() ?? string.Empty
            })
            .ToArray();
    }
}
