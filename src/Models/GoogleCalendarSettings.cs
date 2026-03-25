namespace TimeWidget.Models;

public sealed class GoogleCalendarSettings
{
    public bool Enabled { get; set; } = true;

    public string CalendarId { get; set; } = "primary";

    public int MaxEvents { get; set; } = 4;

    public int RefreshMinutes { get; set; } = 5;

    public string ClientSecretsPath { get; set; } =
        @"%LocalAppData%\TimeWidget\google-oauth-client.json";

    public string TokenStoreDirectory { get; set; } =
        @"%LocalAppData%\TimeWidget\GoogleCalendarToken";

    public string? LoginHint { get; set; }

    public bool ForceAccountSelection { get; set; } = true;
}
