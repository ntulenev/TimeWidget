namespace TimeWidget.Domain.Configuration;

/// <summary>
/// Stores widget positioning and scaling settings.
/// </summary>
public sealed class WidgetPositioningSettings
{
    /// <summary>
    /// Gets or sets the vertical offset, in percent of screen height, used by the center-up action.
    /// </summary>
    public double CenterUpVerticalOffsetPercent { get; set; } = 15;

    /// <summary>
    /// Gets or sets the idle opacity percentage.
    /// </summary>
    public double Opacity { get; set; } = 75;

    /// <summary>
    /// Gets or sets the fixed layout scale percentage.
    /// </summary>
    public double? ScalePercent { get; set; }

    /// <summary>
    /// Gets or sets the target screen-width percentage used for responsive scaling.
    /// </summary>
    public double? ScreenPercent { get; set; }

    /// <summary>
    /// Gets the center-up vertical offset as a ratio.
    /// </summary>
    public double CenterUpVerticalOffsetRatio => CenterUpVerticalOffsetPercent / 100d;

    /// <summary>
    /// Gets the idle opacity as a ratio.
    /// </summary>
    public double IdleOpacity => Opacity / 100d;

    /// <summary>
    /// Applies the configured scale percentage to a base scale.
    /// </summary>
    /// <param name="baseScale">The base scale value.</param>
    /// <returns>The scaled value.</returns>
    public double GetLayoutScale(double baseScale) =>
        ScalePercent.HasValue
            ? baseScale * (ScalePercent.Value / 100d)
            : baseScale;

    /// <summary>
    /// Calculates the layout scale for a specific screen width.
    /// </summary>
    /// <param name="baseScale">The default layout scale.</param>
    /// <param name="widgetWidth">The current widget width.</param>
    /// <param name="screenWidth">The target screen width.</param>
    /// <returns>The calculated layout scale.</returns>
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

        return screenWidth * (ScreenPercent.Value / 100d) / widgetWidth;
    }
}
