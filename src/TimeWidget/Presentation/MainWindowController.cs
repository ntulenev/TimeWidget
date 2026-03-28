using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using Microsoft.Extensions.Options;
using Microsoft.Win32;

using TimeWidget.Domain.Configuration;
using TimeWidget.Domain.Widget;
using TimeWidget.Infrastructure.Windowing;
using TimeWidget.ViewModels;
using TimeWidget.Views;

using Forms = System.Windows.Forms;
using WpfPoint = System.Windows.Point;

namespace TimeWidget.Presentation;

public sealed class MainWindowController
{
    private const double DefaultLayoutScale = 1.15;
    private const double DefaultWidgetWidth = 780;
    private const double HoverOpacityDelta = 0.06;
    private const bool PreferDesktopHostedWallpaperMode = false;

    private MainWindow? _window;
    private MainWindowViewModel? _viewModel;
    private IntPtr _windowHandle;
    private IntPtr _foregroundEventHook;
    private HwndSource? _hwndSource;
    private WindowNativeMethods.WinEventProc? _foregroundEventProc;
    private ScaleTransform? _rootScaleTransform;
    private bool _isDragging;
    private bool _isDesktopHosted;
    private bool _isApplyingLayoutScale;
    private bool _isShellOwnedWallpaper;
    private int _wallpaperRestoreRequestId;
    private WindowNativeMethods.NativePoint _dragOffset;
    private readonly WidgetPositioningSettings _widgetPositioningSettings;
    private readonly double _centerUpVerticalOffsetRatio;
    private readonly double _idleOpacity;
    private readonly double _hoverOpacity;

    public MainWindowController(IOptions<WidgetPositioningSettings> widgetPositioningOptions)
    {
        ArgumentNullException.ThrowIfNull(widgetPositioningOptions);

        _widgetPositioningSettings = widgetPositioningOptions.Value;
        _centerUpVerticalOffsetRatio = _widgetPositioningSettings.GetCenterUpVerticalOffsetRatio();
        _idleOpacity = _widgetPositioningSettings.GetIdleOpacity();
        _hoverOpacity = Math.Min(_idleOpacity + HoverOpacityDelta, 1d);
    }

    public void Attach(MainWindow window, MainWindowViewModel viewModel, ScaleTransform rootScaleTransform)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(rootScaleTransform);

        _window = window;
        _viewModel = viewModel;
        _rootScaleTransform = rootScaleTransform;
        ApplyLayoutScaleForScreen(GetCurrentScreen());

        _window.Topmost = false;
        _window.Opacity = _idleOpacity;

        _viewModel.ShowForEditingRequested += ViewModel_ShowForEditingRequested;
        _viewModel.ReturnToWallpaperModeRequested += ViewModel_ReturnToWallpaperModeRequested;
        _viewModel.CenterUpWidgetRequested += ViewModel_CenterUpWidgetRequested;
        _window.SizeChanged += Window_SizeChanged;
        SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
    }

    public async Task OnLoadedAsync()
    {
        RestoreSavedPositionOrCenter();
        EnsureWidgetVisible();
        await ViewModel.InitializeAsync();
    }

    public void OnClosed()
    {
        SaveWindowPosition();

        WindowNativeMethods.RemoveWinEventHook(_foregroundEventHook);
        _foregroundEventHook = IntPtr.Zero;
        _foregroundEventProc = null;
        SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;

        if (_viewModel is not null)
        {
            _viewModel.ShowForEditingRequested -= ViewModel_ShowForEditingRequested;
            _viewModel.ReturnToWallpaperModeRequested -= ViewModel_ReturnToWallpaperModeRequested;
            _viewModel.CenterUpWidgetRequested -= ViewModel_CenterUpWidgetRequested;
        }

        if (_window is not null)
        {
            _window.SizeChanged -= Window_SizeChanged;
        }
    }

    public void OnSourceInitialized()
    {
        _windowHandle = new WindowInteropHelper(Window).Handle;
        _hwndSource = HwndSource.FromHwnd(_windowHandle);
        _hwndSource?.AddHook(WindowMessageHook);
        RegisterForegroundTracking();
        WindowNativeMethods.ApplyWidgetWindowStyles(_windowHandle);
        ApplyCurrentWidgetMode(bringToFront: false);
    }

    public void HandleDragAreaMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.IsWallpaperMode ||
            e.ButtonState != MouseButtonState.Pressed ||
            _windowHandle == IntPtr.Zero)
        {
            return;
        }

        Window.Focus();
        Keyboard.Focus(Window);

        if (!WindowNativeMethods.TryGetCursorScreenPosition(out var cursorPosition) ||
            !WindowNativeMethods.TryGetWindowRectangle(_windowHandle, out var windowRect))
        {
            return;
        }

        _isDragging = true;
        _dragOffset = new WindowNativeMethods.NativePoint
        {
            X = cursorPosition.X - windowRect.Left,
            Y = cursorPosition.Y - windowRect.Top
        };

        Mouse.Capture((IInputElement)sender);
        e.Handled = true;
    }

    public void HandleDragAreaMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        if (ViewModel.IsWallpaperMode || !_isDragging)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            StopDragging();
            return;
        }

        if (!WindowNativeMethods.TryGetCursorScreenPosition(out var cursorPosition))
        {
            return;
        }

        WindowNativeMethods.MoveWindow(
            _windowHandle,
            cursorPosition.X - _dragOffset.X,
            cursorPosition.Y - _dragOffset.Y);
    }

    public void HandleDragAreaMouseLeftButtonUp()
    {
        if (ViewModel.IsWallpaperMode)
        {
            return;
        }

        StopDragging();
        ApplyLayoutScaleForScreen(GetCurrentScreen());
        SaveWindowPosition();
    }

    public void HandleMouseEnter()
    {
        if (!ViewModel.IsWallpaperMode)
        {
            Window.Opacity = _hoverOpacity;
        }
    }

    public void HandleMouseLeave()
    {
        if (!ViewModel.IsWallpaperMode)
        {
            Window.Opacity = _idleOpacity;
        }
    }

    public void HandleKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (!ViewModel.IsWallpaperMode && e.Key == Key.Escape)
        {
            ViewModel.ReturnToWallpaperModeCommand.Execute(null);
        }
    }

    public void CenterUpOnScreen(Forms.Screen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);

        CenterOnScreen(screen, _centerUpVerticalOffsetRatio);
        EnsureWidgetVisible();
        SaveWindowPosition();
    }

    private MainWindow Window => _window ?? throw new InvalidOperationException("Controller is not attached.");

    private MainWindowViewModel ViewModel =>
        _viewModel ?? throw new InvalidOperationException("Controller is not attached.");

    private void SystemEvents_PowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            Window.Dispatcher.BeginInvoke(ViewModel.HandleSuspend, DispatcherPriority.Background);
            return;
        }

        if (e.Mode != PowerModes.Resume)
        {
            return;
        }

        Window.Dispatcher.BeginInvoke(
            async () => await ViewModel.HandleResumeAsync(),
            DispatcherPriority.Background);
    }

    private void RegisterForegroundTracking()
    {
        _foregroundEventProc = HandleForegroundWindowChanged;
        _foregroundEventHook = WindowNativeMethods.SetForegroundEventHook(_foregroundEventProc);
    }

    private void HandleForegroundWindowChanged(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero || hwnd == _windowHandle)
        {
            return;
        }

        Window.Dispatcher.BeginInvoke(
            SendWallpaperWidgetBehindApps,
            DispatcherPriority.Background);
    }

    private void ViewModel_ShowForEditingRequested(object? sender, EventArgs e)
    {
        ApplyCurrentWidgetMode(bringToFront: true);
    }

    private void ViewModel_ReturnToWallpaperModeRequested(object? sender, EventArgs e)
    {
        ApplyCurrentWidgetMode(bringToFront: false);
        SaveWindowPosition();
    }

    private void ViewModel_CenterUpWidgetRequested(object? sender, EventArgs e)
    {
        CenterUpOnCurrentScreen();
        EnsureWidgetVisible();
        SaveWindowPosition();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!UsesScreenPercentScale || _isApplyingLayoutScale)
        {
            return;
        }

        ApplyLayoutScaleForScreen(GetCurrentScreen());
    }

    private void CenterUpOnCurrentScreen()
    {
        CenterOnScreen(GetCurrentScreen(), _centerUpVerticalOffsetRatio);
    }

    private void CenterOnScreen(Forms.Screen screen, double verticalOffsetRatio)
    {
        ApplyLayoutScaleForScreen(screen);

        var windowBounds = GetCurrentWindowBounds();
        var windowWidth = windowBounds.Right - windowBounds.Left;
        var windowHeight = windowBounds.Bottom - windowBounds.Top;
        var left = screen.Bounds.Left + ((screen.Bounds.Width - windowWidth) / 2);
        var centeredTop = screen.Bounds.Top + ((screen.Bounds.Height - windowHeight) / 2);
        var top = centeredTop - (int)Math.Round(screen.Bounds.Height * verticalOffsetRatio);

        var minTop = screen.Bounds.Top;
        var maxTop = screen.Bounds.Bottom - windowHeight;
        top = Math.Max(minTop, Math.Min(maxTop, top));

        MoveWindowToScreenPixels(left, top);
    }

    private void StopDragging()
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        Mouse.Capture(null);
    }

    private IntPtr WindowMessageHook(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (ViewModel.IsWallpaperMode && !_isDesktopHosted)
        {
            if (msg == WindowNativeMethods.WmSize &&
                wParam.ToInt32() == WindowNativeMethods.SizeMinimized)
            {
                ScheduleWallpaperRestore();
            }
            else if (msg == WindowNativeMethods.WmShowWindow && wParam == IntPtr.Zero)
            {
                ScheduleWallpaperRestore();
            }
            else if (msg == WindowNativeMethods.WmWindowPosChanging && lParam != IntPtr.Zero)
            {
                var windowPos = Marshal.PtrToStructure<WindowNativeMethods.WindowPos>(lParam);
                if ((windowPos.Flags & WindowNativeMethods.SwpHideWindow) != 0)
                {
                    ScheduleWallpaperRestore();
                }
            }
        }

        if (ViewModel.IsWallpaperMode && msg == WindowNativeMethods.WmMouseActivate)
        {
            handled = true;
            return new IntPtr(WindowNativeMethods.MaNoActivate);
        }

        if (ViewModel.IsWallpaperMode && msg == WindowNativeMethods.WmNcHitTest)
        {
            handled = true;
            return new IntPtr(WindowNativeMethods.HtTransparent);
        }

        return IntPtr.Zero;
    }

    private async void ScheduleWallpaperRestore()
    {
        var requestId = ++_wallpaperRestoreRequestId;
        await Task.Delay(200);

        if (requestId != _wallpaperRestoreRequestId ||
            !ViewModel.IsWallpaperMode ||
            _isDesktopHosted ||
            _windowHandle == IntPtr.Zero)
        {
            return;
        }

        if (Window.WindowState == WindowState.Minimized)
        {
            Window.WindowState = WindowState.Normal;
        }

        Window.Show();
        EnsureWidgetVisible();
    }

    private void EnsureWidgetVisible()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        if (ViewModel.IsWallpaperMode)
        {
            WindowNativeMethods.EnsureVisibleWithoutActivation(_windowHandle);

            if (!_isDesktopHosted)
            {
                WindowNativeMethods.SendWindowToBackForWallpaper(_windowHandle);
            }

            return;
        }

        WindowNativeMethods.BringToFrontForEditing(_windowHandle);
    }

    private void SendWallpaperWidgetBehindApps()
    {
        if (!ViewModel.IsWallpaperMode ||
            _windowHandle == IntPtr.Zero ||
            _isDesktopHosted)
        {
            return;
        }

        WindowNativeMethods.SendWindowToBackForWallpaper(_windowHandle);
    }

    private void ApplyCurrentWidgetMode(bool bringToFront)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        StopDragging();
        Window.Opacity = _idleOpacity;
        _wallpaperRestoreRequestId++;

        if (ViewModel.IsWallpaperMode)
        {
            Window.Topmost = false;
            WindowNativeMethods.SetNoActivateStyle(_windowHandle, enabled: true);
            _isDesktopHosted = PreferDesktopHostedWallpaperMode &&
                WindowNativeMethods.TryAttachWindowToDesktopHost(_windowHandle);
            _isShellOwnedWallpaper = !_isDesktopHosted &&
                WindowNativeMethods.TrySetWindowOwnerToShell(_windowHandle);
            Window.Show();
            EnsureWidgetVisible();
            return;
        }

        if (_isDesktopHosted)
        {
            WindowNativeMethods.DetachWindowFromDesktopHost(_windowHandle);
            _isDesktopHosted = false;
        }

        if (_isShellOwnedWallpaper)
        {
            WindowNativeMethods.ClearWindowOwner(_windowHandle);
            _isShellOwnedWallpaper = false;
        }

        WindowNativeMethods.SetNoActivateStyle(_windowHandle, enabled: false);
        Window.Topmost = true;
        Window.Show();

        if (bringToFront)
        {
            EnsureWidgetVisible();
            Window.Activate();
            Window.Focus();
            Keyboard.Focus(Window);
        }
    }

    private void RestoreSavedPositionOrCenter()
    {
        if (ViewModel.TryLoadWindowPlacement(out var savedPlacement))
        {
            var savedScreenPosition = GetSavedScreenPosition(savedPlacement);
            if (IsSavedPositionVisible(savedScreenPosition))
            {
                ApplyLayoutScaleForScreen(Forms.Screen.FromPoint(
                    new System.Drawing.Point(savedScreenPosition.X, savedScreenPosition.Y)));
                MoveWindowToScreenPixels(savedScreenPosition.X, savedScreenPosition.Y);
                return;
            }
        }

        CenterUpOnCurrentScreen();
    }

    private Forms.Screen GetCurrentScreen()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            return Forms.Screen.FromHandle(_windowHandle);
        }

        var source = PresentationSource.FromVisual(Window);
        var topLeft = source?.CompositionTarget?.TransformToDevice.Transform(new WpfPoint(Window.Left, Window.Top))
            ?? new WpfPoint(Window.Left, Window.Top);

        return Forms.Screen.FromPoint(new System.Drawing.Point(
            (int)Math.Round(topLeft.X),
            (int)Math.Round(topLeft.Y)));
    }

    private WindowNativeMethods.NativeRect GetCurrentWindowBounds()
    {
        if (_windowHandle != IntPtr.Zero &&
            WindowNativeMethods.TryGetWindowRectangle(_windowHandle, out var windowRect))
        {
            return windowRect;
        }

        var source = PresentationSource.FromVisual(Window);
        var topLeft = source?.CompositionTarget?.TransformToDevice.Transform(new WpfPoint(Window.Left, Window.Top))
            ?? new WpfPoint(Window.Left, Window.Top);
        var bottomRight = source?.CompositionTarget?.TransformToDevice.Transform(
            new WpfPoint(Window.Left + Window.ActualWidth, Window.Top + Window.ActualHeight))
            ?? new WpfPoint(Window.Left + Window.ActualWidth, Window.Top + Window.ActualHeight);

        return new WindowNativeMethods.NativeRect
        {
            Left = (int)Math.Round(topLeft.X),
            Top = (int)Math.Round(topLeft.Y),
            Right = (int)Math.Round(bottomRight.X),
            Bottom = (int)Math.Round(bottomRight.Y)
        };
    }

    private static bool IsSavedPositionVisible(WindowNativeMethods.NativePoint savedPosition)
    {
        const int visibleWidth = 64;
        const int visibleHeight = 64;

        return savedPosition.X + visibleWidth > SystemParameters.VirtualScreenLeft &&
               savedPosition.Y + visibleHeight > SystemParameters.VirtualScreenTop &&
               savedPosition.X < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth &&
               savedPosition.Y < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
    }

    private void SaveWindowPosition()
    {
        if (_windowHandle == IntPtr.Zero ||
            !WindowNativeMethods.TryGetWindowRectangle(_windowHandle, out var windowRect))
        {
            return;
        }

        ViewModel.SaveScreenPosition(windowRect.Left, windowRect.Top);
    }

    private WindowNativeMethods.NativePoint GetSavedScreenPosition(WidgetPlacement savedPlacement)
    {
        if (savedPlacement.IsPixelUnit)
        {
            return new WindowNativeMethods.NativePoint
            {
                X = (int)Math.Round(savedPlacement.Left),
                Y = (int)Math.Round(savedPlacement.Top)
            };
        }

        var source = PresentationSource.FromVisual(Window);
        var screenPoint = source?.CompositionTarget?.TransformToDevice.Transform(
            new WpfPoint(savedPlacement.Left, savedPlacement.Top))
            ?? new WpfPoint(savedPlacement.Left, savedPlacement.Top);

        return new WindowNativeMethods.NativePoint
        {
            X = (int)Math.Round(screenPoint.X),
            Y = (int)Math.Round(screenPoint.Y)
        };
    }

    private void MoveWindowToScreenPixels(int left, int top)
    {
        var source = PresentationSource.FromVisual(Window);
        var dipPoint = source?.CompositionTarget?.TransformFromDevice.Transform(
            new WpfPoint(left, top))
            ?? new WpfPoint(left, top);

        Window.Left = dipPoint.X;
        Window.Top = dipPoint.Y;

        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        WindowNativeMethods.MoveWindow(_windowHandle, left, top);
    }

    private bool UsesScreenPercentScale =>
        !_widgetPositioningSettings.ScalePercent.HasValue &&
        _widgetPositioningSettings.ScreenPercent.HasValue;

    private void ApplyLayoutScaleForScreen(Forms.Screen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);

        if (_rootScaleTransform is null)
        {
            return;
        }

        var layoutScale = _widgetPositioningSettings.GetLayoutScaleForScreen(
            DefaultLayoutScale,
            GetUnscaledWidgetWidth(),
            screen.Bounds.Width);

        if (Math.Abs(_rootScaleTransform.ScaleX - layoutScale) < 0.0001 &&
            Math.Abs(_rootScaleTransform.ScaleY - layoutScale) < 0.0001)
        {
            return;
        }

        _isApplyingLayoutScale = true;

        try
        {
            _rootScaleTransform.ScaleX = layoutScale;
            _rootScaleTransform.ScaleY = layoutScale;
            Window.UpdateLayout();
        }
        finally
        {
            _isApplyingLayoutScale = false;
        }
    }

    private double GetUnscaledWidgetWidth()
    {
        if (_rootScaleTransform is { ScaleX: > 0 } &&
            Window.ActualWidth > 0)
        {
            return Window.ActualWidth / _rootScaleTransform.ScaleX;
        }

        if (Window.Width > 0)
        {
            return Window.Width;
        }

        return DefaultWidgetWidth;
    }
}
