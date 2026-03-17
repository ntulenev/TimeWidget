using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

using Microsoft.Win32;

using TimeWidget.Infrastructure.Interop;
using TimeWidget.Models;
using TimeWidget.ViewModels;

using Forms = System.Windows.Forms;
using WpfPoint = System.Windows.Point;

namespace TimeWidget.Views;

public partial class MainWindow : Window
{
    private static readonly bool PreferDesktopHostedWallpaperMode = false;

    private const double HoverOpacityDelta = 0.06;

    private readonly MainWindowViewModel _viewModel;
    private readonly double _centerUpVerticalOffsetRatio;
    private readonly double _idleOpacity;
    private readonly double _hoverOpacity;

    private IntPtr _windowHandle;
    private IntPtr _foregroundEventHook;
    private HwndSource? _hwndSource;
    private WindowNativeMethods.WinEventProc? _foregroundEventProc;
    private bool _isDragging;
    private bool _isDesktopHosted;
    private bool _isShellOwnedWallpaper;
    private int _wallpaperRestoreRequestId;
    private WindowNativeMethods.NativePoint _dragOffset;

    public MainWindow(
        MainWindowViewModel viewModel,
        WidgetPositioningSettings widgetPositioningSettings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(widgetPositioningSettings);

        InitializeComponent();

        _viewModel = viewModel;
        _centerUpVerticalOffsetRatio = widgetPositioningSettings.CenterUpVerticalOffsetPercent / 100d;
        _idleOpacity = widgetPositioningSettings.Opacity / 100d;
        _hoverOpacity = Math.Min(_idleOpacity + HoverOpacityDelta, 1d);
        DataContext = viewModel;

        Topmost = false;
        Opacity = _idleOpacity;

        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

        _viewModel.ShowForEditingRequested += ViewModel_ShowForEditingRequested;
        _viewModel.ReturnToWallpaperModeRequested += ViewModel_ReturnToWallpaperModeRequested;
        _viewModel.CenterUpWidgetRequested += ViewModel_CenterUpWidgetRequested;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RestoreSavedPositionOrCenter();
        EnsureWidgetVisible();

        await _viewModel.InitializeAsync();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        SaveWindowPosition();

        WindowNativeMethods.RemoveWinEventHook(_foregroundEventHook);
        _foregroundEventHook = IntPtr.Zero;
        _foregroundEventProc = null;

        SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;

        _viewModel.ShowForEditingRequested -= ViewModel_ShowForEditingRequested;
        _viewModel.ReturnToWallpaperModeRequested -= ViewModel_ReturnToWallpaperModeRequested;
        _viewModel.CenterUpWidgetRequested -= ViewModel_CenterUpWidgetRequested;
        _viewModel.Dispose();
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _windowHandle = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(_windowHandle);
        _hwndSource?.AddHook(WindowMessageHook);
        RegisterForegroundTracking();
        WindowNativeMethods.ApplyWidgetWindowStyles(_windowHandle);
        ApplyCurrentWidgetMode(bringToFront: false);
    }

    private void SystemEvents_PowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            Dispatcher.BeginInvoke(_viewModel.HandleSuspend, DispatcherPriority.Background);
            return;
        }

        if (e.Mode != PowerModes.Resume)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            async () => await _viewModel.HandleResumeAsync(),
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

        Dispatcher.BeginInvoke(
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

    private void CenterUpOnCurrentScreen()
    {
        CenterOnCurrentScreen(verticalOffsetRatio: _centerUpVerticalOffsetRatio);
    }

    private void CenterOnCurrentScreen(double verticalOffsetRatio)
    {
        var screen = GetCurrentScreen();
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

    private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.IsWallpaperMode ||
            e.ButtonState != MouseButtonState.Pressed ||
            _windowHandle == IntPtr.Zero)
        {
            return;
        }

        Focus();
        Keyboard.Focus(this);

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

    private void DragArea_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_viewModel.IsWallpaperMode || !_isDragging)
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

    private void DragArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.IsWallpaperMode)
        {
            return;
        }

        StopDragging();
        SaveWindowPosition();
    }

    private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_viewModel.IsWallpaperMode)
        {
            Opacity = _hoverOpacity;
        }
    }

    private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_viewModel.IsWallpaperMode)
        {
            Opacity = _idleOpacity;
        }
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_viewModel.IsWallpaperMode && e.Key == Key.Escape)
        {
            _viewModel.ReturnToWallpaperModeCommand.Execute(null);
        }
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
        if (_viewModel.IsWallpaperMode && !_isDesktopHosted)
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

        if (_viewModel.IsWallpaperMode && msg == WindowNativeMethods.WmMouseActivate)
        {
            handled = true;
            return new IntPtr(WindowNativeMethods.MaNoActivate);
        }

        if (_viewModel.IsWallpaperMode && msg == WindowNativeMethods.WmNcHitTest)
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
            !_viewModel.IsWallpaperMode ||
            _isDesktopHosted ||
            _windowHandle == IntPtr.Zero)
        {
            return;
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Show();
        EnsureWidgetVisible();
    }

    private void EnsureWidgetVisible()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        if (_viewModel.IsWallpaperMode)
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
        if (!_viewModel.IsWallpaperMode ||
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
        Opacity = _idleOpacity;
        _wallpaperRestoreRequestId++;

        if (_viewModel.IsWallpaperMode)
        {
            Topmost = false;
            WindowNativeMethods.SetNoActivateStyle(_windowHandle, enabled: true);
            _isDesktopHosted = PreferDesktopHostedWallpaperMode &&
                WindowNativeMethods.TryAttachWindowToDesktopHost(_windowHandle);
            _isShellOwnedWallpaper = !_isDesktopHosted &&
                WindowNativeMethods.TrySetWindowOwnerToShell(_windowHandle);
            Show();
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
        Topmost = true;
        Show();

        if (bringToFront)
        {
            EnsureWidgetVisible();
            Activate();
            Focus();
            Keyboard.Focus(this);
        }
    }

    private void RestoreSavedPositionOrCenter()
    {
        if (_viewModel.TryLoadWindowPlacement(out var savedPlacement))
        {
            var savedScreenPosition = GetSavedScreenPosition(savedPlacement);
            if (IsSavedPositionVisible(savedScreenPosition))
            {
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

        var source = PresentationSource.FromVisual(this);
        var topLeft = source?.CompositionTarget?.TransformToDevice.Transform(new WpfPoint(Left, Top))
            ?? new WpfPoint(Left, Top);

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

        var source = PresentationSource.FromVisual(this);
        var topLeft = source?.CompositionTarget?.TransformToDevice.Transform(new WpfPoint(Left, Top))
            ?? new WpfPoint(Left, Top);
        var bottomRight = source?.CompositionTarget?.TransformToDevice.Transform(
            new WpfPoint(Left + ActualWidth, Top + ActualHeight))
            ?? new WpfPoint(Left + ActualWidth, Top + ActualHeight);

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

        _viewModel.SaveScreenPosition(windowRect.Left, windowRect.Top);
    }

    private WindowNativeMethods.NativePoint GetSavedScreenPosition(
        WidgetPlacementSettings savedPlacement)
    {
        if (string.Equals(savedPlacement.Unit, "px", StringComparison.OrdinalIgnoreCase))
        {
            return new WindowNativeMethods.NativePoint
            {
                X = (int)Math.Round(savedPlacement.Left),
                Y = (int)Math.Round(savedPlacement.Top)
            };
        }

        var source = PresentationSource.FromVisual(this);
        var screenPoint = source?.CompositionTarget?.TransformToDevice.Transform(
            new WpfPoint(savedPlacement.Left, savedPlacement.Top))
            ?? new WpfPoint(savedPlacement.Left, savedPlacement.Top);

        return new WindowNativeMethods.NativePoint
        {
            X = (int)Math.Round(screenPoint.X),
            Y = (int)Math.Round(screenPoint.Y)
        };
    }

    private void MoveWindowToDip(double left, double top)
    {
        var source = PresentationSource.FromVisual(this);
        var screenPoint = source?.CompositionTarget?.TransformToDevice.Transform(
            new WpfPoint(left, top))
            ?? new WpfPoint(left, top);

        MoveWindowToScreenPixels(
            (int)Math.Round(screenPoint.X),
            (int)Math.Round(screenPoint.Y));
    }

    private void MoveWindowToScreenPixels(int left, int top)
    {
        var source = PresentationSource.FromVisual(this);
        var dipPoint = source?.CompositionTarget?.TransformFromDevice.Transform(
            new WpfPoint(left, top))
            ?? new WpfPoint(left, top);

        Left = dipPoint.X;
        Top = dipPoint.Y;

        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        WindowNativeMethods.MoveWindow(_windowHandle, left, top);
    }
}
