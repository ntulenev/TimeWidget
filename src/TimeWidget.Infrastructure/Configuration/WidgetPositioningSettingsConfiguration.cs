using Microsoft.Extensions.Options;

using TimeWidget.Domain.Configuration;

namespace TimeWidget.Infrastructure.Configuration;

public sealed class WidgetPositioningSettingsConfiguration :
    IPostConfigureOptions<WidgetPositioningSettings>,
    IValidateOptions<WidgetPositioningSettings>
{
    public void PostConfigure(string? name, WidgetPositioningSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.CenterUpVerticalOffsetPercent = Math.Clamp(options.CenterUpVerticalOffsetPercent, 0, 100);
        options.Opacity = Math.Clamp(options.Opacity, 0, 100);

        if (options.ScalePercent.HasValue)
        {
            options.ScalePercent = Math.Clamp(options.ScalePercent.Value, 50, 200);
        }

        if (options.ScreenPercent.HasValue)
        {
            options.ScreenPercent = Math.Clamp(options.ScreenPercent.Value, 1, 100);
        }

        if (!options.ScalePercent.HasValue && !options.ScreenPercent.HasValue)
        {
            options.ScalePercent = 100;
        }
    }

    public ValidateOptionsResult Validate(string? name, WidgetPositioningSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ScalePercent.HasValue && options.ScalePercent.Value <= 0)
        {
            return ValidateOptionsResult.Fail("WidgetPositioning:ScalePercent must be greater than zero.");
        }

        if (options.ScreenPercent.HasValue && options.ScreenPercent.Value <= 0)
        {
            return ValidateOptionsResult.Fail("WidgetPositioning:ScreenPercent must be greater than zero.");
        }

        return ValidateOptionsResult.Success;
    }
}
