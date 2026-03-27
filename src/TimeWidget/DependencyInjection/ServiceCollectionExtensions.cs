using Microsoft.Extensions.DependencyInjection;

using TimeWidget.Presentation;
using TimeWidget.ViewModels;
using TimeWidget.Views;

namespace TimeWidget;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTimeWidgetPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindowController>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}
