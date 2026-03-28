using System.Runtime.InteropServices;

namespace TimeWidget.Infrastructure.Windowing;

/// <summary>
/// Represents a native screen point.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativePoint
{

    /// <summary>The horizontal coordinate.</summary>
    public int X { readonly get; set; }

    /// <summary>The vertical coordinate.</summary>
    public int Y { readonly get; set; }
}
