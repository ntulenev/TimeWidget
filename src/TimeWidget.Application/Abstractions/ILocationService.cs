using TimeWidget.Domain.Location;

namespace TimeWidget.Application.Abstractions;

/// <summary>
/// Resolves the user's current coordinates.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Attempts to resolve the current coordinates.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The resolved coordinates, or <see langword="null"/> when location is unavailable.</returns>
    Task<Coordinates?> TryGetCoordinatesAsync(CancellationToken cancellationToken);
}
