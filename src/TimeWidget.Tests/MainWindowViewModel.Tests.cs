using System.Reflection;
using System.Windows.Threading;

using FluentAssertions;

using Microsoft.Extensions.Options;

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
using TimeWidget.ViewModels;

namespace TimeWidget.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact(DisplayName = "Constructor should throw when dashboard service is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenDashboardServiceIsNull()
    {
        // Arrange
        var action = () => new MainWindowViewModel(null!);

        // Act
        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Show For Editing Command should raise event and switch mode.")]
    [Trait("Category", "Unit")]
    public void ShowForEditingCommandShouldRaiseEventAndSwitchMode()
    {
        // Arrange
        using var viewModel = new MainWindowViewModel(CreateDashboardService());
        var raised = 0;
        viewModel.ShowForEditingRequested += (_, _) => raised++;

        // Act
        viewModel.ShowForEditingCommand.Execute(null);

        // Assert
        raised.Should().Be(1);
        viewModel.IsWallpaperMode.Should().BeFalse();
        viewModel.EditChromeVisible.Should().BeTrue();
    }

    [Fact(DisplayName = "Return To Wallpaper Mode Command should raise event and switch mode.")]
    [Trait("Category", "Unit")]
    public void ReturnToWallpaperModeCommandShouldRaiseEventAndSwitchMode()
    {
        // Arrange
        using var viewModel = new MainWindowViewModel(CreateDashboardService());
        var raised = 0;
        viewModel.ShowForEditingCommand.Execute(null);
        viewModel.ReturnToWallpaperModeRequested += (_, _) => raised++;

        // Act
        viewModel.ReturnToWallpaperModeCommand.Execute(null);

        // Assert
        raised.Should().Be(1);
        viewModel.IsWallpaperMode.Should().BeTrue();
        viewModel.EditChromeVisible.Should().BeFalse();
    }

    [Fact(DisplayName = "Center Up Widget Command should raise event.")]
    [Trait("Category", "Unit")]
    public void CenterUpWidgetCommandShouldRaiseEvent()
    {
        // Arrange
        using var viewModel = new MainWindowViewModel(CreateDashboardService());
        var raised = 0;
        viewModel.CenterUpWidgetRequested += (_, _) => raised++;

        // Act
        viewModel.CenterUpWidgetCommand.Execute(null);

        // Assert
        raised.Should().Be(1);
    }

    [Fact(DisplayName = "Initialize should apply weather and calendar state.")]
    [Trait("Category", "Unit")]
    public async Task InitializeAsyncShouldApplyWeatherAndCalendarState()
    {
        // Arrange
        using var viewModel = new MainWindowViewModel(CreateDashboardService());

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.TimeText.Should().Be("09:00");
        viewModel.DateText.Should().Be("Saturday, 28 March 2026");
        viewModel.LeftCityTimes.Should().ContainSingle();
        viewModel.LeftCityTimes[0].Name.Should().Be("UTC");
        viewModel.WeatherTemperatureText.Should().Be($"21\u00B0");
        viewModel.WeatherConditionText.Should().Be("Clear");
        viewModel.WeatherLocationText.Should().Be("Berlin");
        viewModel.HasWeatherDetails.Should().BeTrue();
        viewModel.CalendarEvents.Should().ContainSingle();
        viewModel.CalendarEvents[0].Title.Should().Be("Planning");
        viewModel.ShowCompactCalendarSection.Should().BeTrue();
    }

    [Fact(DisplayName = "Handle Suspend should stop background timers.")]
    [Trait("Category", "Unit")]
    public async Task HandleSuspendShouldStopBackgroundTimers()
    {
        // Arrange
        using var viewModel = new MainWindowViewModel(CreateDashboardService());
        await viewModel.InitializeAsync();

        // Act
        viewModel.HandleSuspend();

        // Assert
        GetTimer(viewModel, "_calendarTimer").IsEnabled.Should().BeFalse();
        GetTimer(viewModel, "_weatherTimer").IsEnabled.Should().BeFalse();
    }

    [Fact(DisplayName = "Handle Resume should refresh state and restart timers.")]
    [Trait("Category", "Unit")]
    public async Task HandleResumeAsyncShouldRefreshStateAndRestartTimers()
    {
        // Arrange
        var calendarService = new TrackingCalendarService();
        using var viewModel = new MainWindowViewModel(CreateDashboardService(
            calendarService: calendarService,
            weatherService: new ChangingWeatherService()));
        await viewModel.InitializeAsync();
        viewModel.HandleSuspend();

        // Act
        await viewModel.HandleResumeAsync();

        // Assert
        viewModel.WeatherTemperatureText.Should().Be($"18\u00B0");
        viewModel.WeatherConditionText.Should().Be("Rain");
        viewModel.WeatherLocationText.Should().Be("Hamburg");
        viewModel.CalendarEvents.Should().ContainSingle();
        viewModel.CalendarEvents[0].Title.Should().Be("Background");
        GetTimer(viewModel, "_weatherTimer").IsEnabled.Should().BeTrue();
        GetTimer(viewModel, "_calendarTimer").IsEnabled.Should().BeTrue();
        calendarService.LastInteractionMode.Should().Be(CalendarInteractionMode.Background);
    }

    [Fact(DisplayName = "Refresh Weather Now should apply latest weather state.")]
    [Trait("Category", "Unit")]
    public async Task RefreshWeatherNowAsyncShouldApplyLatestWeatherState()
    {
        // Arrange
        using var viewModel = new MainWindowViewModel(CreateDashboardService(weatherService: new ChangingWeatherService()));

        // Act
        await viewModel.InitializeAsync();
        await viewModel.RefreshWeatherNowAsync();

        // Assert
        viewModel.WeatherTemperatureText.Should().Be($"18\u00B0");
        viewModel.WeatherConditionText.Should().Be("Rain");
        viewModel.WeatherLocationText.Should().Be("Hamburg");
    }

    [Fact(DisplayName = "Refresh Calendar Now should use interactive mode.")]
    [Trait("Category", "Unit")]
    public async Task RefreshCalendarNowAsyncShouldUseInteractiveMode()
    {
        // Arrange
        var calendarService = new TrackingCalendarService();
        using var viewModel = new MainWindowViewModel(CreateDashboardService(calendarService: calendarService));

        // Act
        await viewModel.RefreshCalendarNowAsync();

        // Assert
        calendarService.LastInteractionMode.Should().Be(CalendarInteractionMode.Interactive);
        viewModel.CalendarEvents.Should().ContainSingle();
        viewModel.CalendarEvents[0].Title.Should().Be("Interactive");
    }

    [Fact(DisplayName = "Forgetting calendar authorization should show signed out status.")]
    [Trait("Category", "Unit")]
    public async Task ForgetCalendarAuthorizationAsyncShouldShowSignedOutStatus()
    {
        // Arrange
        using var viewModel = new MainWindowViewModel(CreateDashboardService());

        // Act
        await viewModel.ForgetCalendarAuthorizationAsync();

        // Assert
        viewModel.CalendarStatusText.Should().Be("Google Calendar sign-in removed");
        viewModel.ShowCalendarStatus.Should().BeTrue();
    }

    [Fact(DisplayName = "Save Screen Position And Try Load Window Placement should delegate to placement store.")]
    [Trait("Category", "Unit")]
    public void SaveScreenPositionAndTryLoadWindowPlacementShouldDelegateToPlacementStore()
    {
        // Arrange
        var placementStore = new TrackingWidgetPlacementStore(new WidgetPlacement(10, 20));
        using var viewModel = new MainWindowViewModel(CreateDashboardService(placementStore: placementStore));

        // Act
        viewModel.SaveScreenPosition(30, 40);
        var loaded = viewModel.TryLoadWindowPlacement(out var placement);

        // Assert
        placementStore.SavedPlacement.Should().Be(new WidgetPlacement(30, 40));
        loaded.Should().BeTrue();
        placement.Should().Be(new WidgetPlacement(10, 20));
    }

    [Fact(DisplayName = "Dispose should stop all timers.")]
    [Trait("Category", "Unit")]
    public async Task DisposeShouldStopAllTimers()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(CreateDashboardService());
        await viewModel.InitializeAsync();

        // Act
        viewModel.Dispose();

        // Assert
        GetTimer(viewModel, "_clockTimer").IsEnabled.Should().BeFalse();
        GetTimer(viewModel, "_calendarTimer").IsEnabled.Should().BeFalse();
        GetTimer(viewModel, "_weatherTimer").IsEnabled.Should().BeFalse();
    }

    private static WidgetDashboardService CreateDashboardService(
        ICalendarService? calendarService = null,
        IWeatherService? weatherService = null,
        IWidgetPlacementStore? placementStore = null)
    {
        var now = new DateTimeOffset(2026, 3, 28, 9, 0, 0, TimeSpan.Zero);

        return new WidgetDashboardService(
            new FixedClockService(now),
            calendarService ?? new StubCalendarService(),
            new StubLocationService(),
            weatherService ?? new StubWeatherService(),
            placementStore ?? new StubWidgetPlacementStore(),
            new ClockDisplayBuilder(),
            new CalendarDisplayBuilder(),
            new WeatherDisplayBuilder(),
            Options.Create(new ClockCitiesSettings
            {
                LeftCities =
                {
                    new CityClockDefinition
                    {
                        Name = "UTC",
                        TimeZoneId = TimeZoneInfo.Utc.Id
                    }
                }
            }),
            Options.Create(new GoogleCalendarSettings()));
    }

    private sealed class FixedClockService(DateTimeOffset now) : IClockService
    {
        public DateTimeOffset Now => now;
    }

    private sealed class StubCalendarService : ICalendarService
    {
        public bool IsEnabled => true;

        public Task ForgetAuthorizationAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<CalendarLoadResult> GetUpcomingEventsAsync(
            CalendarInteractionMode interactionMode,
            CancellationToken cancellationToken)
        {
            var now = new DateTimeOffset(2026, 3, 28, 9, 0, 0, TimeSpan.Zero);

            return Task.FromResult(CalendarLoadResult.Success(new CalendarAgenda(
            [
                new CalendarEvent("Planning", now.AddHours(1), now.AddHours(2), false, "accepted")
            ])));
        }
    }

    private sealed class StubLocationService : ILocationService
    {
        public Task<Coordinates?> TryGetCoordinatesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<Coordinates?>(new Coordinates(52.52, 13.40, "Berlin"));
        }
    }

    private sealed class StubWeatherService : IWeatherService
    {
        public Task<WeatherSnapshot> GetCurrentWeatherAsync(
            Coordinates coordinates,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new WeatherSnapshot(21, "Clear", "Berlin"));
        }
    }

    private sealed class StubWidgetPlacementStore : IWidgetPlacementStore
    {
        public void SaveWindowPlacement(WidgetPlacement placement)
        {
        }

        public bool TryLoadWindowPlacement(out WidgetPlacement placement)
        {
            placement = default;
            return false;
        }
    }

    private sealed class ChangingWeatherService : IWeatherService
    {
        public Task<WeatherSnapshot> GetCurrentWeatherAsync(
            Coordinates coordinates,
            CancellationToken cancellationToken)
        {
            _callCount++;
            var snapshot = _callCount == 1
                ? new WeatherSnapshot(21, "Clear", "Berlin")
                : new WeatherSnapshot(18, "Rain", "Hamburg");
            return Task.FromResult(snapshot);
        }

        private int _callCount;
    }

    private sealed class TrackingCalendarService : ICalendarService
    {
        public bool IsEnabled => true;

        public CalendarInteractionMode? LastInteractionMode { get; private set; }

        public Task ForgetAuthorizationAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<CalendarLoadResult> GetUpcomingEventsAsync(
            CalendarInteractionMode interactionMode,
            CancellationToken cancellationToken)
        {
            LastInteractionMode = interactionMode;

            return Task.FromResult(CalendarLoadResult.Success(new CalendarAgenda(
            [
                new CalendarEvent(interactionMode.ToString(), DateTimeOffset.UtcNow, null, false, "accepted")
            ])));
        }
    }

    private sealed class TrackingWidgetPlacementStore(WidgetPlacement placementToLoad) : IWidgetPlacementStore
    {
        public WidgetPlacement? SavedPlacement { get; private set; }

        public void SaveWindowPlacement(WidgetPlacement placement)
        {
            SavedPlacement = placement;
        }

        public bool TryLoadWindowPlacement(out WidgetPlacement placement)
        {
            placement = placementToLoad;
            return true;
        }
    }

    private static DispatcherTimer GetTimer(MainWindowViewModel viewModel, string fieldName)
    {
        return (DispatcherTimer)typeof(MainWindowViewModel)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(viewModel)!;
    }
}


