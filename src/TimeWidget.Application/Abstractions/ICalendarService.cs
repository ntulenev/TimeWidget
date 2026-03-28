using TimeWidget.Application.Calendar;

namespace TimeWidget.Application.Abstractions;

/// <summary>
/// Provides calendar access for the widget.
/// </summary>
public interface ICalendarService
{
    /// <summary>
    /// Gets a value indicating whether calendar integration is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Loads upcoming calendar events for the requested interaction mode.
    /// </summary>
    /// <param name="interactionMode">The interaction context that triggered the refresh.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The loaded calendar state.</returns>
    Task<CalendarLoadResult> GetUpcomingEventsAsync(
        CalendarInteractionMode interactionMode,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes any persisted calendar authorization.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the authorization data has been removed.</returns>
    Task ForgetAuthorizationAsync(CancellationToken cancellationToken);
}
