using System.Runtime.InteropServices;

namespace TimeWidget.Infrastructure.Windowing;

/// <summary>
/// Represents the native <c>WINDOWPOS</c> structure.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct WindowPos
{

    /// <summary>The target window handle.</summary>
    public IntPtr Hwnd { readonly get; set; }

    /// <summary>The window inserted after this handle in the Z order.</summary>
    public IntPtr HwndInsertAfter { readonly get; set; }

    /// <summary>The target X coordinate.</summary>
    public int X { readonly get; set; }

    /// <summary>The target Y coordinate.</summary>
    public int Y { readonly get; set; }

    /// <summary>The target width.</summary>
    public int Cx { readonly get; set; }

    /// <summary>The target height.</summary>
    public int Cy { readonly get; set; }

    /// <summary>The associated positioning flags.</summary>
    public uint Flags { readonly get; set; }
}
