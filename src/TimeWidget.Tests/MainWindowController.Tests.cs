using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

using FluentAssertions;

using Microsoft.Extensions.Options;

using TimeWidget.Application.Abstractions;
using TimeWidget.Application.Calendar;
using TimeWidget.Application.Clock;
using TimeWidget.Application.Weather;
using TimeWidget.Application.Widget;
using TimeWidget.Domain.Clock;
using TimeWidget.Domain.Configuration;
using TimeWidget.Domain.Location;
using TimeWidget.Domain.Weather;
using TimeWidget.Domain.Widget;
using TimeWidget.Infrastructure.Windowing;
using TimeWidget.Presentation;
using TimeWidget.Views;
using TimeWidget.ViewModels;

namespace TimeWidget.Tests;

public sealed class MainWindowControllerTests
{
    [Fact(DisplayName = "MainWindow constructor should throw when view model is null.")]
    [Trait("Category", "Unit")]
    public void MainWindowCtorShouldThrowWhenViewModelIsNull()
    {
        // Arrange
        var exception = RunSta(() =>
        {
            _ = new MainWindow(null!, CreateController());
        });

        // Act
        // Assert
        exception.Should().BeOfType<ArgumentNullException>();
    }

    [Fact(DisplayName = "MainWindow constructor should throw when controller is null.")]
    [Trait("Category", "Unit")]
    public void MainWindowCtorShouldThrowWhenControllerIsNull()
    {
        // Arrange
        var exception = RunSta(() =>
        {
            using var viewModel = new MainWindowViewModel(CreateDashboardService());
            _ = new MainWindow(viewModel, null!);
        });

        // Act
        // Assert
        exception.Should().BeOfType<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor should throw when options is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenOptionsIsNull()
    {
        // Arrange
        var action = () => new MainWindowController(null!, Options.Create(new GoogleCalendarSettings()));

        // Act
        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Attach should throw when window is null.")]
    [Trait("Category", "Unit")]
    public void AttachShouldThrowWhenWindowIsNull()
    {
        // Arrange
        var controller = CreateController();
        using var viewModel = new MainWindowViewModel(CreateDashboardService());
        var rootScaleTransform = new ScaleTransform();

        // Act
        var action = () => controller.Attach(null!, viewModel, rootScaleTransform);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Attach should throw when view model is null.")]
    [Trait("Category", "Unit")]
    public void AttachShouldThrowWhenViewModelIsNull()
    {
        // Arrange
        var controller = CreateController();
        var window = CreateUninitializedMainWindow();
        var rootScaleTransform = new ScaleTransform();

        // Act
        var action = () => controller.Attach(window, null!, rootScaleTransform);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Attach should throw when root scale transform is null.")]
    [Trait("Category", "Unit")]
    public void AttachShouldThrowWhenRootScaleTransformIsNull()
    {
        // Arrange
        var controller = CreateController();
        var window = CreateUninitializedMainWindow();
        using var viewModel = new MainWindowViewModel(CreateDashboardService());

        // Act
        var action = () => controller.Attach(window, viewModel, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Centering on a screen should throw when screen is null.")]
    [Trait("Category", "Unit")]
    public void CenterUpOnScreenShouldThrowWhenScreenIsNull()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var action = () => controller.CenterUpOnScreen(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Calling OnClosed should not throw when controller is not attached.")]
    [Trait("Category", "Unit")]
    public void OnClosedShouldNotThrowWhenControllerIsNotAttached()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var action = () => controller.OnClosed();

        // Assert
        action.Should().NotThrow();
    }

    [Theory(DisplayName = "Public methods should reject use before attach.")]
    [Trait("Category", "Unit")]
    [InlineData("OnLoadedAsync")]
    [InlineData("OnSourceInitialized")]
    [InlineData("HandleDragAreaMouseLeftButtonDown")]
    [InlineData("HandleDragAreaMouseMove")]
    [InlineData("HandleDragAreaMouseLeftButtonUp")]
    [InlineData("HandleMouseEnter")]
    [InlineData("HandleMouseLeave")]
    [InlineData("HandleKeyDown")]
    public async Task PublicMethodsShouldRejectUseBeforeAttach(string methodName)
    {
        // Arrange
        var controller = CreateController();

        // Act
        Func<Task> action = methodName switch
        {
            "OnLoadedAsync" => controller.OnLoadedAsync,
            "OnSourceInitialized" => () =>
            {
                controller.OnSourceInitialized();
                return Task.CompletedTask;
            },
            "HandleDragAreaMouseLeftButtonDown" => () =>
            {
                controller.HandleDragAreaMouseLeftButtonDown(new object(), null!);
                return Task.CompletedTask;
            },
            "HandleDragAreaMouseMove" => () =>
            {
                controller.HandleDragAreaMouseMove(null!);
                return Task.CompletedTask;
            },
            "HandleDragAreaMouseLeftButtonUp" => () =>
            {
                controller.HandleDragAreaMouseLeftButtonUp();
                return Task.CompletedTask;
            },
            "HandleMouseEnter" => () =>
            {
                controller.HandleMouseEnter();
                return Task.CompletedTask;
            },
            "HandleMouseLeave" => () =>
            {
                controller.HandleMouseLeave();
                return Task.CompletedTask;
            },
            "HandleKeyDown" => () =>
            {
                controller.HandleKeyDown(null!);
                return Task.CompletedTask;
            },
            _ => throw new ArgumentOutOfRangeException(nameof(methodName))
        };

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact(DisplayName = "MainWindow centering on a screen should validate screen.")]
    [Trait("Category", "Unit")]
    public void MainWindowCenterUpOnScreenShouldValidateScreen()
    {
        // Arrange
        var window = CreateUninitializedMainWindow();
        var controller = CreateController();
        typeof(MainWindow)
            .GetField("_controller", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(window, controller);

        // Act
        var action = () => window.CenterUpOnScreen(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Window message hook should return no activate for wallpaper mode mouse activate.")]
    [Trait("Category", "Unit")]
    public void WindowMessageHookShouldReturnNoActivateForWallpaperModeMouseActivate()
    {
        // Arrange
        var controller = CreateController();
        using var viewModel = new MainWindowViewModel(CreateDashboardService());
        SetPrivateField(controller, "_viewModel", viewModel);
        var handled = false;

        // Act
        var result = InvokeWindowMessageHook(
            controller,
            WindowNativeMethods.WMMOUSEACTIVATE,
            IntPtr.Zero,
            IntPtr.Zero,
            ref handled);

        // Assert
        handled.Should().BeTrue();
        result.Should().Be(new IntPtr(WindowNativeMethods.MANOACTIVATE));
    }

    [Fact(DisplayName = "Window message hook should return transparent for wallpaper mode hit test.")]
    [Trait("Category", "Unit")]
    public void WindowMessageHookShouldReturnTransparentForWallpaperModeHitTest()
    {
        // Arrange
        var controller = CreateController();
        using var viewModel = new MainWindowViewModel(CreateDashboardService());
        SetPrivateField(controller, "_viewModel", viewModel);
        var handled = false;

        // Act
        var result = InvokeWindowMessageHook(
            controller,
            WindowNativeMethods.WMNCHITTEST,
            IntPtr.Zero,
            IntPtr.Zero,
            ref handled);

        // Assert
        handled.Should().BeTrue();
        result.Should().Be(new IntPtr(WindowNativeMethods.HTTRANSPARENT));
    }

    [Fact(DisplayName = "Window message hook should ignore wallpaper specific messages when editing mode is active.")]
    [Trait("Category", "Unit")]
    public void WindowMessageHookShouldIgnoreWallpaperSpecificMessagesWhenEditingModeIsActive()
    {
        // Arrange
        var controller = CreateController();
        using var viewModel = new MainWindowViewModel(CreateDashboardService());
        viewModel.ShowForEditingCommand.Execute(null);
        SetPrivateField(controller, "_viewModel", viewModel);
        var handled = false;

        // Act
        var result = InvokeWindowMessageHook(
            controller,
            WindowNativeMethods.WMMOUSEACTIVATE,
            IntPtr.Zero,
            IntPtr.Zero,
            ref handled);

        // Assert
        handled.Should().BeFalse();
        result.Should().Be(IntPtr.Zero);
    }

    [Fact(DisplayName = "Saved position visibility should return true for on-screen point.")]
    [Trait("Category", "Unit")]
    public void IsSavedPositionVisibleShouldReturnTrueForOnScreenPoint()
    {
        // Arrange
        var point = new NativePoint
        {
            X = (int)SystemParameters.VirtualScreenLeft + 10,
            Y = (int)SystemParameters.VirtualScreenTop + 10
        };

        // Act
        var visible = InvokeIsSavedPositionVisible(point);

        // Assert
        visible.Should().BeTrue();
    }

    [Fact(DisplayName = "Saved position visibility should return false for off-screen point.")]
    [Trait("Category", "Unit")]
    public void IsSavedPositionVisibleShouldReturnFalseForOffScreenPoint()
    {
        // Arrange
        var point = new NativePoint
        {
            X = (int)SystemParameters.VirtualScreenLeft - 5000,
            Y = (int)SystemParameters.VirtualScreenTop - 5000
        };

        // Act
        var visible = InvokeIsSavedPositionVisible(point);

        // Assert
        visible.Should().BeFalse();
    }

    private static WidgetDashboardService CreateDashboardService()
    {
        var now = new DateTimeOffset(2026, 3, 28, 9, 0, 0, TimeSpan.Zero);

        return new WidgetDashboardService(
            new FixedClockService(now),
            new StubCalendarService(),
            new StubLocationService(),
            new StubWeatherService(),
            new StubWidgetPlacementStore(),
            new ClockDisplayBuilder(),
            new CalendarDisplayBuilder(),
            new WeatherDisplayBuilder(),
            Options.Create(new ClockCitiesSettings()),
            Options.Create(new GoogleCalendarSettings()));
    }

    private static MainWindowController CreateController()
    {
        return new MainWindowController(
            Options.Create(new WidgetPositioningSettings()),
            Options.Create(new GoogleCalendarSettings()));
    }

    private static MainWindow CreateUninitializedMainWindow() =>
        (MainWindow)RuntimeHelpers.GetUninitializedObject(typeof(MainWindow));

    private static IntPtr InvokeWindowMessageHook(
        MainWindowController controller,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        var parameters = new object[] { IntPtr.Zero, message, wParam, lParam, handled };
        var result = typeof(MainWindowController)
            .GetMethod("WindowMessageHook", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(controller, parameters);
        handled = (bool)parameters[4];
        return (IntPtr)result!;
    }

    private static bool InvokeIsSavedPositionVisible(NativePoint point)
    {
        return (bool)typeof(MainWindowController)
            .GetMethod("IsSavedPositionVisible", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [point])!;
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        typeof(MainWindowController)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(instance, value);
    }

    private static Exception? RunSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return exception;
    }

    private sealed class FixedClockService(DateTimeOffset now) : IClockService
    {
        public DateTimeOffset Now => now;
    }

    private sealed class StubCalendarService : ICalendarService
    {
        public bool IsEnabled => true;

        public Task<CalendarLoadResult> GetUpcomingEventsAsync(
            CalendarInteractionMode interactionMode,
            CancellationToken cancellationToken) =>
            Task.FromResult(CalendarLoadResult.FromStatus(CalendarLoadStatus.NoUpcomingEvents));

        public Task ForgetAuthorizationAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubLocationService : ILocationService
    {
        public Task<Coordinates?> TryGetCoordinatesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<Coordinates?>(new Coordinates(52.52, 13.40, "Berlin"));
    }

    private sealed class StubWeatherService : IWeatherService
    {
        public Task<WeatherSnapshot> GetCurrentWeatherAsync(
            Coordinates coordinates,
            CancellationToken cancellationToken) =>
            Task.FromResult(new WeatherSnapshot(21, "Clear", "Berlin"));
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
}


