using System.Runtime.InteropServices;

namespace TimeWidget.Infrastructure.Windowing;

/// <summary>
/// Provides native windowing helpers used to host and position the widget on Windows.
/// </summary>
public static class WindowNativeMethods
{
    /// <summary>Window message sent when a window is shown or hidden.</summary>
    public const int WMSHOWWINDOW = 0x0018;

    /// <summary>Window message sent when the window size changes.</summary>
    public const int WMSIZE = 0x0005;

    /// <summary>Window message sent before a window is activated by mouse input.</summary>
    public const int WMMOUSEACTIVATE = 0x0021;

    /// <summary>Window message used to determine the hit-test result for a point.</summary>
    public const int WMNCHITTEST = 0x0084;

    /// <summary>Window message sent before a window position change is applied.</summary>
    public const int WMWINDOWPOSCHANGING = 0x0046;

    /// <summary>WinEvent identifier raised when the foreground window changes.</summary>
    public const uint EVENTSYSTEMFOREGROUND = 0x0003;

    /// <summary>Hit-test code that makes a window transparent to mouse input.</summary>
    public const int HTTRANSPARENT = -1;

    /// <summary>Mouse-activate result that keeps the window from activating.</summary>
    public const int MANOACTIVATE = 3;

    /// <summary>Window-size code for the minimized state.</summary>
    public const int SIZEMINIMIZED = 1;

    /// <summary>ShowWindow command that displays a window normally.</summary>
    public const int SWSHOW = 5;

    /// <summary>ShowWindow command that shows a window without activating it.</summary>
    public const int SWSHOWNOACTIVATE = 4;

    /// <summary>SetWindowPos flag that preserves the current size.</summary>
    public const uint SWPNOSIZE = 0x0001;

    /// <summary>SetWindowPos flag that preserves the current position.</summary>
    public const uint SWPNOMOVE = 0x0002;

    /// <summary>SetWindowPos flag that preserves the current Z order.</summary>
    public const uint SWPNOZORDER = 0x0004;

    /// <summary>SetWindowPos flag that prevents window activation.</summary>
    public const uint SWPNOACTIVATE = 0x0010;

    /// <summary>SetWindowPos flag that reapplies non-client frame styles.</summary>
    public const uint SWPFRAMECHANGED = 0x0020;

    /// <summary>SetWindowPos flag that shows the window.</summary>
    public const uint SWPSHOWWINDOW = 0x0040;

    /// <summary>SetWindowPos flag that hides the window.</summary>
    public const uint SWPHIDEWINDOW = 0x0080;

    /// <summary>
    /// Attempts to read the current cursor position in screen coordinates.
    /// </summary>
    /// <param name="cursorPosition">When this method returns, contains the cursor position.</param>
    /// <returns><see langword="true"/> when the position was read successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetCursorScreenPosition(out NativePoint cursorPosition)
    {
        return GetCursorPos(out cursorPosition);
    }

    /// <summary>
    /// Attempts to read the bounds of a native window.
    /// </summary>
    /// <param name="windowHandle">The target window handle.</param>
    /// <param name="windowRect">When this method returns, contains the window bounds.</param>
    /// <returns><see langword="true"/> when the bounds were read successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetWindowRectangle(IntPtr windowHandle, out NativeRect windowRect)
    {
        return GetWindowRect(windowHandle, out windowRect);
    }

    /// <summary>
    /// Moves a window without resizing or activating it.
    /// </summary>
    /// <param name="windowHandle">The target window handle.</param>
    /// <param name="x">The new left coordinate in screen pixels.</param>
    /// <param name="y">The new top coordinate in screen pixels.</param>
    public static void MoveWindow(IntPtr windowHandle, int x, int y)
    {
        _ = SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            x,
            y,
            0,
            0,
            SWPNOSIZE | SWPNOACTIVATE | SWPNOZORDER);
    }

    /// <summary>
    /// Ensures a window is visible without taking focus.
    /// </summary>
    /// <param name="windowHandle">The target window handle.</param>
    public static void EnsureVisibleWithoutActivation(IntPtr windowHandle)
    {
        _ = ShowWindow(windowHandle, SWSHOWNOACTIVATE);
        _ = SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SWPNOMOVE | SWPNOSIZE | SWPNOACTIVATE | SWPSHOWWINDOW);
    }

    /// <summary>
    /// Applies the tool-window styles used by the widget.
    /// </summary>
    /// <param name="windowHandle">The target window handle.</param>
    public static void ApplyWidgetWindowStyles(IntPtr windowHandle)
    {
        var exStyle = GetWindowLongPtr(windowHandle, GWL_EX_STYLE).ToInt64();
        exStyle &= ~WS_EX_APP_WINDOW;
        exStyle |= WS_EX_TOOL_WINDOW;
        _ = SetWindowLongPtr(windowHandle, GWL_EX_STYLE, new IntPtr(exStyle));
    }

    /// <summary>
    /// Enables or disables the no-activate extended window style.
    /// </summary>
    /// <param name="windowHandle">The target window handle.</param>
    /// <param name="enabled">A value indicating whether the style should be enabled.</param>
    public static void SetNoActivateStyle(IntPtr windowHandle, bool enabled)
    {
        var exStyle = GetWindowLongPtr(windowHandle, GWL_EX_STYLE).ToInt64();
        exStyle = enabled
            ? exStyle | WS_EX_NO_ACTIVATE
            : exStyle & ~WS_EX_NO_ACTIVATE;

        _ = SetWindowLongPtr(windowHandle, GWL_EX_STYLE, new IntPtr(exStyle));
        _ = SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SWPNOMOVE | SWPNOSIZE | SWPNOZORDER | SWPNOACTIVATE | SWPFRAMECHANGED);
    }

    /// <summary>
    /// Attempts to set the shell window as the owner of the specified window.
    /// </summary>
    /// <param name="windowHandle">The target window handle.</param>
    /// <returns><see langword="true"/> when the owner was updated successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TrySetWindowOwnerToShell(IntPtr windowHandle)
    {
        var shellWindow = GetShellWindow();
        if (shellWindow == IntPtr.Zero)
        {
            return false;
        }

        SetKernelLastError(0);
        var previousOwner = SetWindowLongPtr(windowHandle, GWL_HWND_PARENT, shellWindow);
        if (previousOwner == IntPtr.Zero && Marshal.GetLastWin32Error() != 0)
        {
            return false;
        }

        _ = SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SWPNOMOVE | SWPNOSIZE | SWPNOZORDER | SWPNOACTIVATE | SWPFRAMECHANGED);
        return true;
    }

    /// <summary>
    /// Clears the owner of the specified window.
    /// </summary>
    /// <param name="windowHandle">The target window handle.</param>
    public static void ClearWindowOwner(IntPtr windowHandle)
    {
        SetKernelLastError(0);
        _ = SetWindowLongPtr(windowHandle, GWL_HWND_PARENT, IntPtr.Zero);
        _ = SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SWPNOMOVE | SWPNOSIZE | SWPNOZORDER | SWPNOACTIVATE | SWPFRAMECHANGED);
    }

    /// <summary>
    /// Registers a WinEvent hook for foreground-window changes.
    /// </summary>
    /// <param name="callback">The callback invoked for foreground-window events.</param>
    /// <returns>The hook handle, or <see cref="IntPtr.Zero"/> when registration fails.</returns>
    public static IntPtr SetForegroundEventHook(WinEventProc callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return SetWinEventHook(
            EVENTSYSTEMFOREGROUND,
            EVENTSYSTEMFOREGROUND,
            IntPtr.Zero,
            callback,
            0,
            0,
            WINEVENT_OUT_OF_CONTEXT | WINEVENT_SKIP_OWN_PROCESS);
    }

    /// <summary>
    /// Removes a previously registered WinEvent hook.
    /// </summary>
    /// <param name="hookHandle">The hook handle to remove.</param>
    public static void RemoveWinEventHook(IntPtr hookHandle)
    {
        if (hookHandle != IntPtr.Zero)
        {
            _ = UnhookWinEvent(hookHandle);
        }
    }

    /// <summary>
    /// Attempts to attach the widget window to a desktop host window.
    /// </summary>
    /// <param name="windowHandle">The widget window handle.</param>
    /// <returns><see langword="true"/> when the window was attached successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryAttachWindowToDesktopHost(IntPtr windowHandle)
    {
        foreach (var desktopHost in GetDesktopHostCandidates())
        {
            if (desktopHost == IntPtr.Zero)
            {
                continue;
            }

            if (TryAttachWindowToHost(windowHandle, desktopHost))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Detaches the widget window from its desktop host.
    /// </summary>
    /// <param name="windowHandle">The widget window handle.</param>
    public static void DetachWindowFromDesktopHost(IntPtr windowHandle)
    {
        _ = SetParent(windowHandle, IntPtr.Zero);
        SetParentStyle(windowHandle, isChildWindow: false);
        _ = ShowWindow(windowHandle, SWSHOW);
        _ = SetWindowPos(
            windowHandle,
            _hwndNotTopMost,
            0,
            0,
            0,
            0,
            SWPNOMOVE | SWPNOSIZE | SWPSHOWWINDOW);
    }

    /// <summary>
    /// Sends the widget window behind normal application windows for wallpaper mode.
    /// </summary>
    /// <param name="windowHandle">The widget window handle.</param>
    public static void SendWindowToBackForWallpaper(IntPtr windowHandle)
    {
        _ = ShowWindow(windowHandle, SWSHOWNOACTIVATE);
        _ = SetWindowPos(
            windowHandle,
            _hwndBottom,
            0,
            0,
            0,
            0,
            SWPNOMOVE | SWPNOSIZE | SWPNOACTIVATE | SWPSHOWWINDOW);
    }

    /// <summary>
    /// Brings the widget window to the front for interactive editing.
    /// </summary>
    /// <param name="windowHandle">The widget window handle.</param>
    public static void BringToFrontForEditing(IntPtr windowHandle)
    {
        _ = ShowWindow(windowHandle, SWSHOW);
        _ = SetWindowPos(
            windowHandle,
            _hwndTopMost,
            0,
            0,
            0,
            0,
            SWPNOMOVE | SWPNOSIZE | SWPSHOWWINDOW);
        _ = SetForegroundWindow(windowHandle);
    }

    private static void SetParentStyle(IntPtr windowHandle, bool isChildWindow)
    {
        var style = GetWindowLongPtr(windowHandle, GWL_STYLE).ToInt64();

        if (isChildWindow)
        {
            style |= WS_CHILD;
            style &= ~WS_POPUP;
        }
        else
        {
            style &= ~WS_CHILD;
            style |= WS_POPUP;
        }

        _ = SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr(style));
        _ = SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SWPNOMOVE | SWPNOSIZE | SWPNOZORDER | SWPNOACTIVATE | SWPFRAMECHANGED);
    }

    private static bool TryAttachWindowToHost(IntPtr windowHandle, IntPtr desktopHost)
    {
        SetParentStyle(windowHandle, isChildWindow: true);
        SetKernelLastError(0);
        var previousParent = SetParent(windowHandle, desktopHost);
        if (previousParent == IntPtr.Zero && Marshal.GetLastWin32Error() != 0)
        {
            SetParentStyle(windowHandle, isChildWindow: false);
            return false;
        }

        _ = ShowWindow(windowHandle, SWSHOWNOACTIVATE);
        _ = SetWindowPos(
            windowHandle,
            _hwndBottom,
            0,
            0,
            0,
            0,
            SWPNOMOVE | SWPNOSIZE | SWPNOACTIVATE | SWPSHOWWINDOW);
        return true;
    }

    private static IEnumerable<IntPtr> GetDesktopHostCandidates()
    {
        var progman = FindWindow("Progman", null);
        if (progman != IntPtr.Zero)
        {
            _ = SendMessageTimeout(
                progman,
                PROGMAN_SPAWN_WORKER_MESSAGE,
                IntPtr.Zero,
                IntPtr.Zero,
                SEND_MESSAGE_TIMEOUT_NORMAL,
                1000,
                out _);
        }

        var workerWindow = IntPtr.Zero;

        _ = EnumWindows(
            (topLevelWindow, _) =>
            {
                var shellView = FindWindowEx(
                    topLevelWindow,
                    IntPtr.Zero,
                    "SHELLDLL_DefView",
                    null);
                if (shellView == IntPtr.Zero)
                {
                    return true;
                }

                workerWindow = FindWindowEx(IntPtr.Zero, topLevelWindow, "WorkerW", null);
                return workerWindow == IntPtr.Zero;
            },
            IntPtr.Zero);

        if (workerWindow != IntPtr.Zero)
        {
            yield return workerWindow;
        }

        if (progman != IntPtr.Zero)
        {
            yield return progman;
        }
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(
        IntPtr hWndParent,
        IntPtr hWndChildAfter,
        string lpszClass,
        string? lpszWindow);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out NativePoint lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventProc lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
    private static extern void SetKernelLastError(uint dwErrCode);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const int GWL_STYLE = -16;
    private const int GWL_EX_STYLE = -20;
    private const int GWL_HWND_PARENT = -8;

    private static readonly IntPtr _hwndBottom = new(1);
    private static readonly IntPtr _hwndNotTopMost = new(-2);
    private static readonly IntPtr _hwndTopMost = new(-1);

    private const long WS_CHILD = 0x40000000L;
    private const long WS_POPUP = unchecked((int)0x80000000);
    private const long WS_EX_APP_WINDOW = 0x00040000L;
    private const long WS_EX_NO_ACTIVATE = 0x08000000L;
    private const long WS_EX_TOOL_WINDOW = 0x00000080L;
    private const uint PROGMAN_SPAWN_WORKER_MESSAGE = 0x052C;
    private const uint SEND_MESSAGE_TIMEOUT_NORMAL = 0x0000;
    private const uint WINEVENT_OUT_OF_CONTEXT = 0x0000;
    private const uint WINEVENT_SKIP_OWN_PROCESS = 0x0002;
}
