using Microsoft.Extensions.DependencyInjection;

using TimeWidget.Presentation;
using TimeWidget.ViewModels;
using TimeWidget.Views;

namespace TimeWidget.DependencyInjection;

/// <summary>
/// Registers presentation-layer services for the widget.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds presentation services used by the WPF application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTimeWidgetPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindowController>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}
