namespace TimeWidget.Domain.Configuration;

public enum GoogleCalendarMode
{
    Compact = 0,
    FullCalendar = 1
}

public sealed class GoogleCalendarSettings
{
    public bool Enabled { get; set; } = true;

    public string CalendarId { get; set; } = "primary";

    public GoogleCalendarMode Mode { get; set; } = GoogleCalendarMode.Compact;

    public int MaxEventsCompact { get; set; } = 3;

    public int MaxEventsFull { get; set; } = 8;

    public int RefreshMinutes { get; set; } = 5;

    public string ClientSecretsPath { get; set; } =
        @"%LocalAppData%\TimeWidget\google-oauth-client.json";

    public string TokenStoreDirectory { get; set; } =
        @"%LocalAppData%\TimeWidget\GoogleCalendarToken";

    public string? LoginHint { get; set; }

    public bool ForceAccountSelection { get; set; } = true;

    public bool IsFullCalendarMode => Mode == GoogleCalendarMode.FullCalendar;

    public int ActiveMaxEvents => IsFullCalendarMode ? MaxEventsFull : MaxEventsCompact;
}
