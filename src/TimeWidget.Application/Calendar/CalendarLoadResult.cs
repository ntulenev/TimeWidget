using TimeWidget.Domain.Calendar;

namespace TimeWidget.Application.Calendar;

/// <summary>
/// Represents the result of loading calendar data.
/// </summary>
public sealed class CalendarLoadResult
{
    private CalendarLoadResult(CalendarLoadStatus status, CalendarAgenda agenda)
    {
        Status = status;
        Agenda = agenda;
    }

    /// <summary>
    /// Gets the load status.
    /// </summary>
    public CalendarLoadStatus Status { get; }

    /// <summary>
    /// Gets the loaded agenda.
    /// </summary>
    public CalendarAgenda Agenda { get; }

    /// <summary>
    /// Creates a successful load result.
    /// </summary>
    /// <param name="agenda">The loaded agenda.</param>
    /// <returns>A successful load result.</returns>
    public static CalendarLoadResult Success(CalendarAgenda agenda) =>
        new(CalendarLoadStatus.Success, agenda);

    /// <summary>
    /// Creates a load result for a non-success status.
    /// </summary>
    /// <param name="status">The resulting status.</param>
    /// <returns>A load result with an empty agenda.</returns>
    public static CalendarLoadResult FromStatus(CalendarLoadStatus status) =>
        new(status, CalendarAgenda.Empty);
}
