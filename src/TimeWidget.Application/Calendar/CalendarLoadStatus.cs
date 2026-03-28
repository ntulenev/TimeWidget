namespace TimeWidget.Application.Calendar;

/// <summary>
/// Describes the result of a calendar refresh.
/// </summary>
public enum CalendarLoadStatus
{
    /// <summary>
    /// The calendar data was loaded successfully.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Calendar integration is disabled.
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// The OAuth client secrets file is missing.
    /// </summary>
    ClientSecretsMissing = 2,

    /// <summary>
    /// User authorization is required before events can be loaded.
    /// </summary>
    AuthorizationRequired = 3,

    /// <summary>
    /// The previous authorization was removed.
    /// </summary>
    AuthorizationRemoved = 4,

    /// <summary>
    /// Access to the requested calendar was denied.
    /// </summary>
    AccessDenied = 5,

    /// <summary>
    /// Calendar data could not be loaded because the service is unavailable.
    /// </summary>
    Unavailable = 6,

    /// <summary>
    /// No upcoming events were found.
    /// </summary>
    NoUpcomingEvents = 7
}
