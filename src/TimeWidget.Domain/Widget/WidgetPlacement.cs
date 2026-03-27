namespace TimeWidget.Domain.Widget;

public readonly record struct WidgetPlacement(double Left, double Top, string Unit = "px")
{
    public const string PixelUnit = "px";

    public bool IsPixelUnit => string.Equals(Unit, PixelUnit, StringComparison.OrdinalIgnoreCase);

    public WidgetPlacement WithUnit(string unit) =>
        new(Left, Top, string.IsNullOrWhiteSpace(unit) ? PixelUnit : unit.Trim());
}
