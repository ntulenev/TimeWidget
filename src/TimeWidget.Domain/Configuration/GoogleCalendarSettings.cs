namespace TimeWidget.Domain.Configuration;

/// <summary>
/// Defines the supported calendar display modes.
/// </summary>
public enum GoogleCalendarMode
{
    /// <summary>
    /// Shows the compact calendar layout.
    /// </summary>
    Compact = 0,

    /// <summary>
    /// Shows the full calendar layout.
    /// </summary>
    FullCalendar = 1
}

/// <summary>
/// Stores Google Calendar integration settings.
/// </summary>
public sealed class GoogleCalendarSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether calendar integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Google Calendar identifier.
    /// </summary>
    public string CalendarId { get; set; } = "primary";

    /// <summary>
    /// Gets or sets the active calendar display mode.
    /// </summary>
    public GoogleCalendarMode Mode { get; set; } = GoogleCalendarMode.Compact;

    /// <summary>
    /// Gets or sets the maximum number of events shown in compact mode.
    /// </summary>
    public int MaxEventsCompact { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum number of events shown in full mode.
    /// </summary>
    public int MaxEventsFull { get; set; } = 8;

    /// <summary>
    /// Gets or sets the refresh interval, in minutes.
    /// </summary>
    public int RefreshMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the path to the Google OAuth client secrets file.
    /// </summary>
    public string ClientSecretsPath { get; set; } =
        @"%LocalAppData%\TimeWidget\google-oauth-client.json";

    /// <summary>
    /// Gets or sets the directory used to store OAuth tokens.
    /// </summary>
    public string TokenStoreDirectory { get; set; } =
        @"%LocalAppData%\TimeWidget\GoogleCalendarToken";

    /// <summary>
    /// Gets or sets the preferred account email shown during sign-in.
    /// </summary>
    public string? LoginHint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the account picker should always be shown.
    /// </summary>
    public bool ForceAccountSelection { get; set; } = true;

    /// <summary>
    /// Gets a value indicating whether full calendar mode is active.
    /// </summary>
    public bool IsFullCalendarMode => Mode == GoogleCalendarMode.FullCalendar;

    /// <summary>
    /// Gets the active event limit for the selected mode.
    /// </summary>
    public int ActiveMaxEvents => IsFullCalendarMode ? MaxEventsFull : MaxEventsCompact;
}
