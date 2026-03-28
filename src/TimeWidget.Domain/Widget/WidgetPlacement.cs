namespace TimeWidget.Domain.Widget;

/// <summary>
/// Represents the saved widget position and its unit.
/// </summary>
/// <param name="Left">The horizontal coordinate.</param>
/// <param name="Top">The vertical coordinate.</param>
/// <param name="Unit">The coordinate unit.</param>
public readonly record struct WidgetPlacement(double Left, double Top, string Unit = "px")
{
    /// <summary>
    /// Gets the pixel unit identifier.
    /// </summary>
    public static string PixelUnit => "px";

    /// <summary>
    /// Gets a value indicating whether the placement unit is pixels.
    /// </summary>
    public bool IsPixelUnit => string.Equals(Unit, PixelUnit, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a copy of the placement with the specified unit.
    /// </summary>
    /// <param name="unit">The unit to apply.</param>
    /// <returns>A new placement with the updated unit.</returns>
    public WidgetPlacement WithUnit(string unit) =>
        new(Left, Top, string.IsNullOrWhiteSpace(unit) ? PixelUnit : unit.Trim());
}
