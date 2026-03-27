namespace TimeWidget.Application.Calendar;

public enum CalendarLoadStatus
{
    Success = 0,
    Disabled = 1,
    ClientSecretsMissing = 2,
    AuthorizationRequired = 3,
    AuthorizationRemoved = 4,
    AccessDenied = 5,
    Unavailable = 6,
    NoUpcomingEvents = 7
}
