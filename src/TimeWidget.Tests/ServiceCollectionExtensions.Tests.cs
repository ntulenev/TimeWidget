using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using TimeWidget.Presentation;
using TimeWidget.ViewModels;
using TimeWidget.Views;

namespace TimeWidget.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact(DisplayName = "Add TimeWidget Presentation should throw when services is null.")]
    [Trait("Category", "Unit")]
    public void AddTimeWidgetPresentationShouldThrowWhenServicesIsNull()
    {
        // Arrange
        var action = () => ServiceCollectionExtensions.AddTimeWidgetPresentation(null!);

        // Act
        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Add TimeWidget Presentation should register singleton services.")]
    [Trait("Category", "Unit")]
    public void AddTimeWidgetPresentationShouldRegisterSingletonServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddTimeWidgetPresentation();

        // Assert
        returnedServices.Should().BeSameAs(services);
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(MainWindowViewModel) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(MainWindowController) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(MainWindow) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
    }
}

