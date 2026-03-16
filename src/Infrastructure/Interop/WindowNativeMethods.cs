using System.Runtime.InteropServices;

namespace TimeWidget.Infrastructure.Interop;

internal static class WindowNativeMethods
{
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const int GwlHwndParent = -8;

    private static readonly IntPtr HwndBottom = new(1);
    private static readonly IntPtr HwndNotTopMost = new(-2);
    private static readonly IntPtr HwndTopMost = new(-1);

    internal const int WmShowWindow = 0x0018;
    internal const int WmSize = 0x0005;
    internal const int WmMouseActivate = 0x0021;
    internal const int WmNcHitTest = 0x0084;
    internal const int WmWindowPosChanging = 0x0046;
    internal const uint EventSystemForeground = 0x0003;

    internal const int HtTransparent = -1;
    internal const int MaNoActivate = 3;
    internal const int SizeMinimized = 1;

    internal const int SwShow = 5;
    internal const int SwShownoActivate = 4;

    internal const uint SwpNoSize = 0x0001;
    internal const uint SwpNoMove = 0x0002;
    internal const uint SwpNoZOrder = 0x0004;
    internal const uint SwpNoActivate = 0x0010;
    internal const uint SwpFrameChanged = 0x0020;
    internal const uint SwpShowWindow = 0x0040;
    internal const uint SwpHideWindow = 0x0080;

    private const long WsChild = 0x40000000L;
    private const long WsPopup = unchecked((int)0x80000000);
    private const long WsExAppWindow = 0x00040000L;
    private const long WsExNoActivate = 0x08000000L;
    private const long WsExToolWindow = 0x00000080L;
    private const uint ProgmanSpawnWorkerMessage = 0x052C;
    private const uint SendMessageTimeoutNormal = 0x0000;
    private const uint WineventOutOfContext = 0x0000;
    private const uint WineventSkipOwnProcess = 0x0002;

    internal static bool TryGetCursorScreenPosition(out NativePoint cursorPosition)
    {
        return GetCursorPos(out cursorPosition);
    }

    internal static bool TryGetWindowRectangle(IntPtr windowHandle, out NativeRect windowRect)
    {
        return GetWindowRect(windowHandle, out windowRect);
    }

    internal static void MoveWindow(IntPtr windowHandle, int x, int y)
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

    internal static void EnsureVisibleWithoutActivation(IntPtr windowHandle)
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

    internal static void ApplyWidgetWindowStyles(IntPtr windowHandle)
    {
        var exStyle = GetWindowLongPtr(windowHandle, GwlExStyle).ToInt64();
        exStyle &= ~WsExAppWindow;
        exStyle |= WsExToolWindow;
        _ = SetWindowLongPtr(windowHandle, GwlExStyle, new IntPtr(exStyle));
    }

    internal static void SetNoActivateStyle(IntPtr windowHandle, bool enabled)
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

    internal static bool TrySetWindowOwnerToShell(IntPtr windowHandle)
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

    internal static void ClearWindowOwner(IntPtr windowHandle)
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

    internal static IntPtr SetForegroundEventHook(WinEventProc callback)
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

    internal static void RemoveWinEventHook(IntPtr hookHandle)
    {
        if (hookHandle != IntPtr.Zero)
        {
            _ = UnhookWinEvent(hookHandle);
        }
    }

    internal static bool TryAttachWindowToDesktopHost(IntPtr windowHandle)
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

    internal static void DetachWindowFromDesktopHost(IntPtr windowHandle)
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

    internal static void SendWindowToBackForWallpaper(IntPtr windowHandle)
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

    internal static void BringToFrontForEditing(IntPtr windowHandle)
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

    internal delegate void WinEventProc(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime);

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePoint
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowPos
    {
        internal IntPtr Hwnd;
        internal IntPtr HwndInsertAfter;
        internal int X;
        internal int Y;
        internal int Cx;
        internal int Cy;
        internal uint Flags;
    }
}
