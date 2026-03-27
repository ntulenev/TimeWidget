namespace TimeWidget.Domain.Configuration;

public sealed class WidgetPositioningSettings
{
    public double CenterUpVerticalOffsetPercent { get; set; } = 15;

    public double Opacity { get; set; } = 75;

    public double ScalePercent { get; set; } = 100;

    public double GetCenterUpVerticalOffsetRatio() => CenterUpVerticalOffsetPercent / 100d;

    public double GetIdleOpacity() => Opacity / 100d;

    public double GetLayoutScale(double baseScale) => baseScale * (ScalePercent / 100d);
}
