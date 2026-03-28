using Microsoft.Extensions.Options;

using TimeWidget.Domain.Configuration;

namespace TimeWidget.Infrastructure.Configuration;

/// <summary>
/// Normalizes and validates Google Calendar settings after binding.
/// </summary>
public sealed class GoogleCalendarSettingsConfiguration :
    IPostConfigureOptions<GoogleCalendarSettings>,
    IValidateOptions<GoogleCalendarSettings>
{
    /// <inheritdoc />
    public void PostConfigure(string? name, GoogleCalendarSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.CalendarId = string.IsNullOrWhiteSpace(options.CalendarId)
            ? "primary"
            : options.CalendarId.Trim();
        if (!Enum.IsDefined(options.Mode))
        {
            options.Mode = GoogleCalendarMode.Compact;
        }

        options.MaxEventsCompact = Math.Clamp(options.MaxEventsCompact, 1, 8);
        options.MaxEventsFull = Math.Clamp(options.MaxEventsFull, 1, 20);
        options.RefreshMinutes = Math.Clamp(options.RefreshMinutes, 1, 60);
        options.ClientSecretsPath = options.ClientSecretsPath?.Trim() ?? string.Empty;
        options.TokenStoreDirectory = options.TokenStoreDirectory?.Trim() ?? string.Empty;
        options.LoginHint = string.IsNullOrWhiteSpace(options.LoginHint)
            ? null
            : options.LoginHint.Trim();
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, GoogleCalendarSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Enabled && string.IsNullOrWhiteSpace(options.CalendarId))
        {
            return ValidateOptionsResult.Fail("GoogleCalendar:CalendarId must be configured.");
        }

        return ValidateOptionsResult.Success;
    }
}
