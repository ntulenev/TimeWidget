using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using TimeWidget.Application.Calendar;
using TimeWidget.Application.Clock;
using TimeWidget.Application.Weather;
using TimeWidget.Application.Widget;

namespace TimeWidget.Application.Tests;

public sealed class ApplicationServiceCollectionExtensionsTests
{
    [Fact(DisplayName = "Add TimeWidget Application should throw when services is null.")]
    [Trait("Category", "Unit")]
    public void AddTimeWidgetApplicationShouldThrowWhenServicesIsNull()
    {
        // Arrange
        var action = () => ApplicationServiceCollectionExtensions.AddTimeWidgetApplication(null!);

        // Act
        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Add TimeWidget Application should register singleton services.")]
    [Trait("Category", "Unit")]
    public void AddTimeWidgetApplicationShouldRegisterSingletonServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddTimeWidgetApplication();

        // Assert
        returnedServices.Should().BeSameAs(services);
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(ClockDisplayBuilder) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(CalendarDisplayBuilder) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(WeatherDisplayBuilder) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(WidgetDashboardService) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
    }
}

