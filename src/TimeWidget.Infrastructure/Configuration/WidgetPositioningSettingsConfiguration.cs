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
        options.ScalePercent = Math.Clamp(options.ScalePercent, 50, 200);
    }

    public ValidateOptionsResult Validate(string? name, WidgetPositioningSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ScalePercent <= 0)
        {
            return ValidateOptionsResult.Fail("WidgetPositioning:ScalePercent must be greater than zero.");
        }

        return ValidateOptionsResult.Success;
    }
}
