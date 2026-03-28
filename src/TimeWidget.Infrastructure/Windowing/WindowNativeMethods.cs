using System.Runtime.InteropServices;

namespace TimeWidget.Infrastructure.Windowing;

/// <summary>
/// Provides native windowing helpers used to host and position the widget on Windows.
/// </summary>
public static class WindowNativeMethods
{
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const int GwlHwndParent = -8;

    private static readonly IntPtr HwndBottom = new(1);
    private static readonly IntPtr HwndNotTopMost = new(-2);
    private static readonly IntPtr HwndTopMost = new(-1);

    /// <summary>Window message sent when a window is shown or hidden.</summary>
    public const int WmShowWindow = 0x0018;

    /// <summary>Window message sent when the window size changes.</summary>
    public const int WmSize = 0x0005;

    /// <summary>Window message sent before a window is activated by mouse input.</summary>
    public const int WmMouseActivate = 0x0021;

    /// <summary>Window message used to determine the hit-test result for a point.</summary>
    public const int WmNcHitTest = 0x0084;

    /// <summary>Window message sent before a window position change is applied.</summary>
    public const int WmWindowPosChanging = 0x0046;

    /// <summary>WinEvent identifier raised when the foreground window changes.</summary>
    public const uint EventSystemForeground = 0x0003;

    /// <summary>Hit-test code that makes a window transparent to mouse input.</summary>
    public const int HtTransparent = -1;

    /// <summary>Mouse-activate result that keeps the window from activating.</summary>
    public const int MaNoActivate = 3;

    /// <summary>Window-size code for the minimized state.</summary>
    public const int SizeMinimized = 1;

    /// <summary>ShowWindow command that displays a window normally.</summary>
    public const int SwShow = 5;

    /// <summary>ShowWindow command that shows a window without activating it.</summary>
    public const int SwShownoActivate = 4;

    /// <summary>SetWindowPos flag that preserves the current size.</summary>
    public const uint SwpNoSize = 0x0001;

    /// <summary>SetWindowPos flag that preserves the current position.</summary>
    public const uint SwpNoMove = 0x0002;

    /// <summary>SetWindowPos flag that preserves the current Z order.</summary>
    public const uint SwpNoZOrder = 0x0004;

    /// <summary>SetWindowPos flag that prevents window activation.</summary>
    public const uint SwpNoActivate = 0x0010;

    /// <summary>SetWindowPos flag that reapplies non-client frame styles.</summary>
    public const uint SwpFrameChanged = 0x0020;

    /// <summary>SetWindowPos flag that shows the window.</summary>
    public const uint SwpShowWindow = 0x0040;

    /// <summary>SetWindowPos flag that hides the window.</summary>
    public const uint SwpHideWindow = 0x0080;

    private const long WsChild = 0x40000000L;
    private const long WsPopup = unchecked((int)0x80000000);
    private const long WsExAppWindow = 0x00040000L;
    private const long WsExNoActivate = 0x08000000L;
    private const long WsExToolWindow = 0x00000080L;
    private const uint ProgmanSpawnWorkerMessage = 0x052C;
    private const uint SendMessageTimeoutNormal = 0x0000;
    private const uint WineventOutOfContext = 0x0000;
    private const uint WineventSkipOwnProcess = 0x0002;

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
            SwpNoSize | SwpNoActivate | SwpNoZOrder);
    }

    /// <summary>
    /// Ensures a window is visible without taking focus.
    /// </summary>
    /// <param name="windowHandle">The target window handle.</param>
    public static void EnsureVisibleWithoutActivation(IntPtr windowHandle)
    {
        _ = ShowWindow(windowHandle, SwShownoActivate);
        _ = SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
    }

    /// <summary>
    /// Applies the tool-window styles used by the widget.
    /// </summary>
    /// <param name="windowHandle">The target window handle.</param>
    public static void ApplyWidgetWindowStyles(IntPtr windowHandle)
    {
        var exStyle = GetWindowLongPtr(windowHandle, GwlExStyle).ToInt64();
        exStyle &= ~WsExAppWindow;
        exStyle |= WsExToolWindow;
        _ = SetWindowLongPtr(windowHandle, GwlExStyle, new IntPtr(exStyle));
    }

    /// <summary>
    /// Enables or disables the no-activate extended window style.
    /// </summary>
    /// <param name="windowHandle">The target window handle.</param>
    /// <param name="enabled">A value indicating whether the style should be enabled.</param>
    public static void SetNoActivateStyle(IntPtr windowHandle, bool enabled)
    {
        var exStyle = GetWindowLongPtr(windowHandle, GwlExStyle).ToInt64();
        exStyle = enabled
            ? exStyle | WsExNoActivate
            : exStyle & ~WsExNoActivate;

        _ = SetWindowLongPtr(windowHandle, GwlExStyle, new IntPtr(exStyle));
        _ = SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
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
        var previousOwner = SetWindowLongPtr(windowHandle, GwlHwndParent, shellWindow);
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
            SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
        return true;
    }

    /// <summary>
    /// Clears the owner of the specified window.
    /// </summary>
    /// <param name="windowHandle">The target window handle.</param>
    public static void ClearWindowOwner(IntPtr windowHandle)
    {
        SetKernelLastError(0);
        _ = SetWindowLongPtr(windowHandle, GwlHwndParent, IntPtr.Zero);
        _ = SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }

    /// <summary>
    /// Registers a WinEvent hook for foreground-window changes.
    /// </summary>
    /// <param name="callback">The callback invoked for foreground-window events.</param>
    /// <returns>The hook handle, or <see cref="IntPtr.Zero"/> when registration fails.</returns>
    public static IntPtr SetForegroundEventHook(WinEventProc callback)
    {
        return SetWinEventHook(
            EventSystemForeground,
            EventSystemForeground,
            IntPtr.Zero,
            callback,
            0,
            0,
            WineventOutOfContext | WineventSkipOwnProcess);
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
        _ = ShowWindow(windowHandle, SwShow);
        _ = SetWindowPos(
            windowHandle,
            HwndNotTopMost,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpShowWindow);
    }

    /// <summary>
    /// Sends the widget window behind normal application windows for wallpaper mode.
    /// </summary>
    /// <param name="windowHandle">The widget window handle.</param>
    public static void SendWindowToBackForWallpaper(IntPtr windowHandle)
    {
        _ = ShowWindow(windowHandle, SwShownoActivate);
        _ = SetWindowPos(
            windowHandle,
            HwndBottom,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
    }

    /// <summary>
    /// Brings the widget window to the front for interactive editing.
    /// </summary>
    /// <param name="windowHandle">The widget window handle.</param>
    public static void BringToFrontForEditing(IntPtr windowHandle)
    {
        _ = ShowWindow(windowHandle, SwShow);
        _ = SetWindowPos(
            windowHandle,
            HwndTopMost,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpShowWindow);
        _ = SetForegroundWindow(windowHandle);
    }

    private static void SetParentStyle(IntPtr windowHandle, bool isChildWindow)
    {
        var style = GetWindowLongPtr(windowHandle, GwlStyle).ToInt64();

        if (isChildWindow)
        {
            style |= WsChild;
            style &= ~WsPopup;
        }
        else
        {
            style &= ~WsChild;
            style |= WsPopup;
        }

        _ = SetWindowLongPtr(windowHandle, GwlStyle, new IntPtr(style));
        _ = SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
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

        _ = ShowWindow(windowHandle, SwShownoActivate);
        _ = SetWindowPos(
            windowHandle,
            HwndBottom,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
        return true;
    }

    private static IEnumerable<IntPtr> GetDesktopHostCandidates()
    {
        var progman = FindWindow("Progman", null);
        if (progman != IntPtr.Zero)
        {
            _ = SendMessageTimeout(
                progman,
                ProgmanSpawnWorkerMessage,
                IntPtr.Zero,
                IntPtr.Zero,
                SendMessageTimeoutNormal,
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

    /// <summary>
    /// Represents a callback invoked for WinEvent notifications.
    /// </summary>
    /// <param name="hWinEventHook">The hook handle that raised the event.</param>
    /// <param name="eventType">The WinEvent identifier.</param>
    /// <param name="hwnd">The related window handle.</param>
    /// <param name="idObject">The object identifier.</param>
    /// <param name="idChild">The child identifier.</param>
    /// <param name="idEventThread">The thread that generated the event.</param>
    /// <param name="dwmsEventTime">The event timestamp.</param>
    public delegate void WinEventProc(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime);

    /// <summary>
    /// Represents a native screen point.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NativePoint
    {
        /// <summary>The horizontal coordinate.</summary>
        public int X;

        /// <summary>The vertical coordinate.</summary>
        public int Y;
    }

    /// <summary>
    /// Represents a native window rectangle.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeRect
    {
        /// <summary>The left coordinate.</summary>
        public int Left;

        /// <summary>The top coordinate.</summary>
        public int Top;

        /// <summary>The right coordinate.</summary>
        public int Right;

        /// <summary>The bottom coordinate.</summary>
        public int Bottom;
    }

    /// <summary>
    /// Represents the native <c>WINDOWPOS</c> structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPos
    {
        /// <summary>The target window handle.</summary>
        public IntPtr Hwnd;

        /// <summary>The window inserted after this handle in the Z order.</summary>
        public IntPtr HwndInsertAfter;

        /// <summary>The target X coordinate.</summary>
        public int X;

        /// <summary>The target Y coordinate.</summary>
        public int Y;

        /// <summary>The target width.</summary>
        public int Cx;

        /// <summary>The target height.</summary>
        public int Cy;

        /// <summary>The associated positioning flags.</summary>
        public uint Flags;
    }
}
