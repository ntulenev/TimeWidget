using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using TimeWidget.Application.Abstractions;
using TimeWidget.Domain.Clock;
using TimeWidget.Domain.Configuration;
using TimeWidget.Infrastructure.Calendar;
using TimeWidget.Infrastructure.Clock;
using TimeWidget.Infrastructure.Configuration;
using TimeWidget.Infrastructure.Location;
using TimeWidget.Infrastructure.Persistence;

namespace TimeWidget.Infrastructure.Tests;

public sealed class InfrastructureServiceCollectionExtensionsTests
{
    [Fact(DisplayName = "Add TimeWidget Infrastructure should throw when services is null.")]
    [Trait("Category", "Unit")]
    public void AddTimeWidgetInfrastructureShouldThrowWhenServicesIsNull()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var action = () => InfrastructureServiceCollectionExtensions.AddTimeWidgetInfrastructure(
            null!,
            configuration);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Add TimeWidget Infrastructure should throw when configuration is null.")]
    [Trait("Category", "Unit")]
    public void AddTimeWidgetInfrastructureShouldThrowWhenConfigurationIsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddTimeWidgetInfrastructure(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Add TimeWidget Infrastructure should register core services.")]
    [Trait("Category", "Unit")]
    public void AddTimeWidgetInfrastructureShouldRegisterCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var returnedServices = services.AddTimeWidgetInfrastructure(configuration);

        // Assert
        returnedServices.Should().BeSameAs(services);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IPostConfigureOptions<ClockCitiesSettings>) &&
            descriptor.ImplementationType == typeof(ClockCitiesSettingsConfiguration));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IPostConfigureOptions<GoogleCalendarSettings>) &&
            descriptor.ImplementationType == typeof(GoogleCalendarSettingsConfiguration));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IValidateOptions<GoogleCalendarSettings>) &&
            descriptor.ImplementationType == typeof(GoogleCalendarSettingsConfiguration));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IClockService) &&
            descriptor.ImplementationType == typeof(SystemClockService) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICalendarService) &&
            descriptor.ImplementationType == typeof(GoogleCalendarService) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ILocationService) &&
            descriptor.ImplementationType == typeof(WindowsLocationService) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IWidgetPlacementStore) &&
            descriptor.ImplementationType == typeof(JsonWidgetPlacementStore) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IWeatherService));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IValidateOptions<WidgetPositioningSettings>) &&
            descriptor.ImplementationType == typeof(WidgetPositioningSettingsConfiguration));
    }
}

