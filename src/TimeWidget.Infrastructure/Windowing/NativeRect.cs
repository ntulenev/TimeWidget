using System.Runtime.InteropServices;

namespace TimeWidget.Infrastructure.Windowing;

/// <summary>
/// Represents a native window rectangle.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeRect
{

    /// <summary>The left coordinate.</summary>
    public int Left { readonly get; set; }

    /// <summary>The top coordinate.</summary>
    public int Top { readonly get; set; }

    /// <summary>The right coordinate.</summary>
    public int Right { readonly get; set; }

    /// <summary>The bottom coordinate.</summary>
    public int Bottom { readonly get; set; }
}
