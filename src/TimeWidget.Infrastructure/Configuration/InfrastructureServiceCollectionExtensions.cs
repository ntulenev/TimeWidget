using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using TimeWidget.Application.Abstractions;
using TimeWidget.Domain.Clock;
using TimeWidget.Domain.Configuration;
using TimeWidget.Infrastructure.Calendar;
using TimeWidget.Infrastructure.Clock;
using TimeWidget.Infrastructure.Location;
using TimeWidget.Infrastructure.Persistence;
using TimeWidget.Infrastructure.Weather;

namespace TimeWidget.Infrastructure.Configuration;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddTimeWidgetInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IPostConfigureOptions<ClockCitiesSettings>, ClockCitiesSettingsConfiguration>();
        services.AddSingleton<IPostConfigureOptions<GoogleCalendarSettings>, GoogleCalendarSettingsConfiguration>();
        services.AddSingleton<IValidateOptions<GoogleCalendarSettings>, GoogleCalendarSettingsConfiguration>();
        services.AddSingleton<IPostConfigureOptions<WidgetPositioningSettings>, WidgetPositioningSettingsConfiguration>();
        services.AddSingleton<IValidateOptions<WidgetPositioningSettings>, WidgetPositioningSettingsConfiguration>();

        services.AddOptions<ClockCitiesSettings>()
            .Bind(configuration.GetSection("ClockCities"))
            .ValidateOnStart();
        services.AddOptions<WidgetPositioningSettings>()
            .Bind(configuration.GetSection("WidgetPositioning"))
            .ValidateOnStart();
        services.AddOptions<GoogleCalendarSettings>()
            .Bind(configuration.GetSection("GoogleCalendar"))
            .ValidateOnStart();

        services.AddSingleton<IClockService, SystemClockService>();
        services.AddSingleton<ICalendarService, GoogleCalendarService>();
        services.AddSingleton<ILocationService, WindowsLocationService>();
        services.AddSingleton<IWidgetPlacementStore, JsonWidgetPlacementStore>();
        services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>(client =>
        {
            client.BaseAddress = new Uri("https://api.open-meteo.com/");
        });

        return services;
    }
}
