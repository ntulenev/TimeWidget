using Microsoft.Extensions.DependencyInjection;

using TimeWidget.Application.Calendar;
using TimeWidget.Application.Clock;
using TimeWidget.Application.Weather;

namespace TimeWidget.Application.Widget;

/// <summary>
/// Registers application-layer services for the widget.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Adds application services used by the widget.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTimeWidgetApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddSingleton<ClockDisplayBuilder>()
            .AddSingleton<CalendarDisplayBuilder>()
            .AddSingleton<WeatherDisplayBuilder>()
            .AddSingleton<WidgetDashboardService>();
    }
}
