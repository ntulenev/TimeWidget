using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

using TimeWidget.Application.Abstractions;
using TimeWidget.Application.Calendar;
using TimeWidget.Application.Clock;
using TimeWidget.Application.Weather;
using TimeWidget.Application.Widget;
using TimeWidget.Domain.Calendar;
using TimeWidget.Domain.Clock;
using TimeWidget.Domain.Configuration;
using TimeWidget.Domain.Location;
using TimeWidget.Domain.Weather;
using TimeWidget.Domain.Widget;

namespace TimeWidget.Application.Tests;

public sealed class WidgetDashboardServiceTests
{
    [Theory(DisplayName = "Constructor should throw when dependency is null.")]
    [Trait("Category", "Unit")]
    [InlineData("clockService")]
    [InlineData("calendarService")]
    [InlineData("locationService")]
    [InlineData("weatherService")]
    [InlineData("placementStore")]
    [InlineData("clockDisplayBuilder")]
    [InlineData("calendarDisplayBuilder")]
    [InlineData("weatherDisplayBuilder")]
    [InlineData("clockCitiesOptions")]
    [InlineData("googleCalendarOptions")]
    public void CtorShouldThrowWhenDependencyIsNull(string dependencyName)
    {
        // Arrange
        var dependencies = CreateDependencies();

        // Act
        switch (dependencyName)
        {
            case "clockService":
                dependencies.ClockService = null;
                break;
            case "calendarService":
                dependencies.CalendarService = null;
                break;
            case "locationService":
                dependencies.LocationService = null;
                break;
            case "weatherService":
                dependencies.WeatherService = null;
                break;
            case "placementStore":
                dependencies.PlacementStore = null;
                break;
            case "clockDisplayBuilder":
                dependencies.ClockDisplayBuilder = null;
                break;
            case "calendarDisplayBuilder":
                dependencies.CalendarDisplayBuilder = null;
                break;
            case "weatherDisplayBuilder":
                dependencies.WeatherDisplayBuilder = null;
                break;
            case "clockCitiesOptions":
                dependencies.ClockCitiesOptions = null;
                break;
            case "googleCalendarOptions":
                dependencies.GoogleCalendarOptions = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dependencyName), dependencyName, null);
        }

        // Assert
        var action = () => dependencies.CreateService();

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The properties and clock display should use configured services.")]
    [Trait("Category", "Unit")]
    public void PropertiesAndClockDisplayShouldUseConfiguredServices()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 3, 28, 9, 0, 0, TimeSpan.Zero);
        var dashboardService = CreateDashboardService(
            now: now,
            calendarEnabled: false,
            clockCitiesSettings: new ClockCitiesSettings
            {
                LeftCities =
                {
                    new CityClockDefinition
                    {
                        Name = "UTC",
                        TimeZoneId = TimeZoneInfo.Utc.Id
                    }
                }
            },
            googleCalendarSettings: new GoogleCalendarSettings
            {
                RefreshMinutes = 7
            });

        // Act
        var display = dashboardService.GetClockDisplayState();

        // Assert
        dashboardService.IsCalendarEnabled.Should().BeFalse();
        dashboardService.CalendarRefreshInterval.Should().Be(TimeSpan.FromMinutes(7));
        display.TimeText.Should().Be("09:00");
        display.LeftCityTimes.Should().ContainSingle();
        display.LeftCityTimes[0].Name.Should().Be("UTC");
    }

    [Fact(DisplayName = "Refreshing weather should cache coordinates and return success.")]
    [Trait("Category", "Unit")]
    public async Task RefreshWeatherAsyncShouldCacheCoordinatesAndReturnSuccess()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var coordinates = new Coordinates(52.52, 13.40, "Berlin");
        var locationCalls = 0;
        var weatherCalls = 0;
        var locationService = new Mock<ILocationService>(MockBehavior.Strict);
        locationService.Setup(service => service.TryGetCoordinatesAsync(cts.Token))
            .Callback(() => locationCalls++)
            .ReturnsAsync(coordinates);
        var weatherService = new Mock<IWeatherService>(MockBehavior.Strict);
        weatherService.Setup(service => service.GetCurrentWeatherAsync(coordinates, cts.Token))
            .Callback(() => weatherCalls++)
            .ReturnsAsync(new WeatherSnapshot(21, "Clear", "Berlin"));
        var dashboardService = CreateDashboardService(
            locationService: locationService.Object,
            weatherService: weatherService.Object);

        // Act
        var first = await dashboardService.RefreshWeatherAsync(cts.Token);
        var second = await dashboardService.RefreshWeatherAsync(cts.Token);

        // Assert
        first.Succeeded.Should().BeTrue();
        first.DisplayState.LocationText.Should().Be("Berlin");
        second.Succeeded.Should().BeTrue();
        locationCalls.Should().Be(1);
        weatherCalls.Should().Be(2);
    }

    [Fact(DisplayName = "Refreshing weather should return unavailable when weather fails.")]
    [Trait("Category", "Unit")]
    public async Task RefreshWeatherAsyncShouldReturnUnavailableWhenWeatherFails()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var coordinates = new Coordinates(52.52, 13.40, "Berlin");
        var weatherCalls = 0;
        var locationService = new Mock<ILocationService>(MockBehavior.Strict);
        locationService.Setup(service => service.TryGetCoordinatesAsync(cts.Token))
            .ReturnsAsync(coordinates);
        var weatherService = new Mock<IWeatherService>(MockBehavior.Strict);
        weatherService.Setup(service => service.GetCurrentWeatherAsync(coordinates, cts.Token))
            .Callback(() => weatherCalls++)
            .ThrowsAsync(new HttpRequestException("boom"));
        var dashboardService = CreateDashboardService(
            locationService: locationService.Object,
            weatherService: weatherService.Object);

        // Act
        var result = await dashboardService.RefreshWeatherAsync(cts.Token);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.DisplayState.LocationText.Should().Be("Weather unavailable");
        result.DisplayState.HasDetails.Should().BeFalse();
        weatherCalls.Should().Be(1);
    }

    [Fact(DisplayName = "Refreshing weather should return location unavailable when coordinates cannot be resolved.")]
    [Trait("Category", "Unit")]
    public async Task RefreshWeatherAsyncShouldReturnLocationUnavailableWhenCoordinatesCannotBeResolved()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var locationCalls = 0;
        var weatherCalls = 0;
        var locationService = new Mock<ILocationService>(MockBehavior.Strict);
        locationService.Setup(service => service.TryGetCoordinatesAsync(cts.Token))
            .Callback(() => locationCalls++)
            .ReturnsAsync((Coordinates?)null);
        var weatherService = new Mock<IWeatherService>(MockBehavior.Strict);
        var dashboardService = CreateDashboardService(
            locationService: locationService.Object,
            weatherService: weatherService.Object);

        // Act
        var result = await dashboardService.RefreshWeatherAsync(cts.Token);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.DisplayState.LocationText.Should().Be("Enable Windows location");
        locationCalls.Should().Be(1);
        weatherCalls.Should().Be(0);
    }

    [Fact(DisplayName = "Refreshing the calendar should build display using clock now.")]
    [Trait("Category", "Unit")]
    public async Task RefreshCalendarAsyncShouldBuildDisplayUsingClockNow()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var now = new DateTimeOffset(2026, 3, 28, 9, 0, 0, TimeSpan.Zero);
        var getUpcomingEventsCalls = 0;
        var calendarEvent = new CalendarEvent(
            "1:1",
            now.AddHours(1),
            now.AddHours(2),
            false,
            "accepted");
        var calendarService = new Mock<ICalendarService>(MockBehavior.Strict);
        calendarService.SetupGet(service => service.IsEnabled).Returns(true);
        calendarService.Setup(service => service.GetUpcomingEventsAsync(
                CalendarInteractionMode.Interactive,
                cts.Token))
            .Callback(() => getUpcomingEventsCalls++)
            .ReturnsAsync(CalendarLoadResult.Success(new CalendarAgenda([calendarEvent])));
        calendarService.Setup(service => service.ForgetAuthorizationAsync(cts.Token))
            .Returns(Task.CompletedTask);
        var dashboardService = CreateDashboardService(
            now: now,
            calendarService: calendarService.Object);

        // Act
        var state = await dashboardService.RefreshCalendarAsync(
            CalendarInteractionMode.Interactive,
            cts.Token);

        // Assert
        state.Events.Should().ContainSingle();
        state.Events[0].Title.Should().Be("1:1");
        state.Events[0].ScheduleText.Should().Be($"Today - {calendarEvent.Start.ToLocalTime():HH:mm}");
        state.ShowEvents.Should().BeTrue();
        getUpcomingEventsCalls.Should().Be(1);
    }

    [Fact(DisplayName = "Forgetting calendar authorization should return signed out state.")]
    [Trait("Category", "Unit")]
    public async Task ForgetCalendarAuthorizationAsyncShouldReturnSignedOutState()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var forgetAuthorizationCalls = 0;
        var calendarService = new Mock<ICalendarService>(MockBehavior.Strict);
        calendarService.SetupGet(service => service.IsEnabled).Returns(true);
        calendarService.Setup(service => service.ForgetAuthorizationAsync(cts.Token))
            .Callback(() => forgetAuthorizationCalls++)
            .Returns(Task.CompletedTask);
        var dashboardService = CreateDashboardService(calendarService: calendarService.Object);

        // Act
        var state = await dashboardService.ForgetCalendarAuthorizationAsync(cts.Token);

        // Assert
        state.StatusText.Should().Be("Google Calendar sign-in removed");
        state.ShowStatus.Should().BeTrue();
        forgetAuthorizationCalls.Should().Be(1);
    }

    [Fact(DisplayName = "Saving the placement should persist pixel unit.")]
    [Trait("Category", "Unit")]
    public void SavePlacementShouldPersistPixelUnit()
    {
        // Arrange
        var savePlacementCalls = 0;
        var placementStore = new Mock<IWidgetPlacementStore>(MockBehavior.Strict);
        placementStore.Setup(store => store.SaveWindowPlacement(
                It.Is<WidgetPlacement>(placement =>
                    placement.Left == 10 &&
                    placement.Top == 20 &&
                    placement.IsPixelUnit)))
            .Callback(() => savePlacementCalls++);
        WidgetPlacement ignoredPlacement;
        placementStore.Setup(store => store.TryLoadWindowPlacement(out ignoredPlacement))
            .Returns(false);
        var dashboardService = CreateDashboardService(placementStore: placementStore.Object);

        // Act
        dashboardService.SavePlacement(new WidgetPlacement(10, 20, "dip"));

        // Assert
        savePlacementCalls.Should().Be(1);
    }

    [Fact(DisplayName = "Loading the placement should forward store result.")]
    [Trait("Category", "Unit")]
    public void TryLoadPlacementShouldForwardStoreResult()
    {
        // Arrange
        var expectedPlacement = new WidgetPlacement(10, 20);
        var placementStore = new Mock<IWidgetPlacementStore>(MockBehavior.Strict);
        placementStore.Setup(store => store.TryLoadWindowPlacement(out expectedPlacement))
            .Returns(true);
        var dashboardService = CreateDashboardService(placementStore: placementStore.Object);

        // Act
        var loaded = dashboardService.TryLoadPlacement(out var actualPlacement);

        // Assert
        loaded.Should().BeTrue();
        actualPlacement.Should().Be(expectedPlacement);
    }

    private static WidgetDashboardService CreateDashboardService(
        DateTimeOffset? now = null,
        ICalendarService? calendarService = null,
        ILocationService? locationService = null,
        IWeatherService? weatherService = null,
        IWidgetPlacementStore? placementStore = null,
        bool calendarEnabled = true,
        ClockCitiesSettings? clockCitiesSettings = null,
        GoogleCalendarSettings? googleCalendarSettings = null)
    {
        var effectiveNow = now ?? new DateTimeOffset(2026, 3, 28, 9, 0, 0, TimeSpan.Zero);
        var clockService = new Mock<IClockService>(MockBehavior.Strict);
        clockService.SetupGet(service => service.Now).Returns(effectiveNow);

        var calendar = calendarService ?? CreateCalendarServiceMock(calendarEnabled).Object;
        var location = locationService ?? CreateLocationServiceMock().Object;
        var weather = weatherService ?? CreateWeatherServiceMock().Object;
        var store = placementStore ?? CreatePlacementStoreMock().Object;

        return new WidgetDashboardService(
            clockService.Object,
            calendar,
            location,
            weather,
            store,
            new ClockDisplayBuilder(),
            new CalendarDisplayBuilder(),
            new WeatherDisplayBuilder(),
            Options.Create(clockCitiesSettings ?? new ClockCitiesSettings()),
            Options.Create(googleCalendarSettings ?? new GoogleCalendarSettings()));
    }

    private static Mock<ICalendarService> CreateCalendarServiceMock(bool isEnabled)
    {
        var calendarService = new Mock<ICalendarService>(MockBehavior.Strict);
        calendarService.SetupGet(service => service.IsEnabled).Returns(isEnabled);
        return calendarService;
    }

    private static Mock<ILocationService> CreateLocationServiceMock()
    {
        return new Mock<ILocationService>(MockBehavior.Strict);
    }

    private static Mock<IWeatherService> CreateWeatherServiceMock()
    {
        return new Mock<IWeatherService>(MockBehavior.Strict);
    }

    private static Mock<IWidgetPlacementStore> CreatePlacementStoreMock()
    {
        return new Mock<IWidgetPlacementStore>(MockBehavior.Strict);
    }

    private sealed class DashboardDependencies
    {
        public IClockService? ClockService { get; set; } = new Mock<IClockService>(MockBehavior.Strict).Object;
        public ICalendarService? CalendarService { get; set; } = new Mock<ICalendarService>(MockBehavior.Strict).Object;
        public ILocationService? LocationService { get; set; } = new Mock<ILocationService>(MockBehavior.Strict).Object;
        public IWeatherService? WeatherService { get; set; } = new Mock<IWeatherService>(MockBehavior.Strict).Object;
        public IWidgetPlacementStore? PlacementStore { get; set; } = new Mock<IWidgetPlacementStore>(MockBehavior.Strict).Object;
        public ClockDisplayBuilder? ClockDisplayBuilder { get; set; } = new();
        public CalendarDisplayBuilder? CalendarDisplayBuilder { get; set; } = new();
        public WeatherDisplayBuilder? WeatherDisplayBuilder { get; set; } = new();
        public IOptions<ClockCitiesSettings>? ClockCitiesOptions { get; set; } =
            Options.Create(new ClockCitiesSettings());
        public IOptions<GoogleCalendarSettings>? GoogleCalendarOptions { get; set; } =
            Options.Create(new GoogleCalendarSettings());

        public WidgetDashboardService CreateService()
        {
            return new WidgetDashboardService(
                ClockService!,
                CalendarService!,
                LocationService!,
                WeatherService!,
                PlacementStore!,
                ClockDisplayBuilder!,
                CalendarDisplayBuilder!,
                WeatherDisplayBuilder!,
                ClockCitiesOptions!,
                GoogleCalendarOptions!);
        }
    }

    private static DashboardDependencies CreateDependencies() => new();
}


