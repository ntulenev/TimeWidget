using Microsoft.Extensions.DependencyInjection;

using TimeWidget.Application.Calendar;
using TimeWidget.Application.Clock;
using TimeWidget.Application.Weather;

namespace TimeWidget.Application.Widget;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddTimeWidgetApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ClockDisplayBuilder>();
        services.AddSingleton<CalendarDisplayBuilder>();
        services.AddSingleton<WeatherDisplayBuilder>();
        services.AddSingleton<WidgetDashboardService>();

        return services;
    }
}
