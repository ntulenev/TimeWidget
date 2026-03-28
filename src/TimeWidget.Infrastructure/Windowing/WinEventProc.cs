namespace TimeWidget.Infrastructure.Windowing;

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
