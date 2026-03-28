namespace TimeWidget.Domain.Configuration;

public sealed class WidgetPositioningSettings
{
    public double CenterUpVerticalOffsetPercent { get; set; } = 15;

    public double Opacity { get; set; } = 75;

    public double? ScalePercent { get; set; }

    public double? ScreenPercent { get; set; }

    public double GetCenterUpVerticalOffsetRatio() => CenterUpVerticalOffsetPercent / 100d;

    public double GetIdleOpacity() => Opacity / 100d;

    public double GetLayoutScale(double baseScale) =>
        ScalePercent.HasValue
            ? baseScale * (ScalePercent.Value / 100d)
            : baseScale;

    public double GetLayoutScaleForScreen(
        double baseScale,
        double widgetWidth,
        double screenWidth)
    {
        if (ScalePercent.HasValue)
        {
            return GetLayoutScale(baseScale);
        }

        if (!ScreenPercent.HasValue)
        {
            return baseScale;
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(widgetWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(screenWidth);

        return (screenWidth * (ScreenPercent.Value / 100d)) / widgetWidth;
    }
}
