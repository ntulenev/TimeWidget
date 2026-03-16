using TimeWidget.Models;

namespace TimeWidget.Abstractions;

public interface ILocationService
{
    Task<Coordinates?> TryGetCoordinatesAsync(CancellationToken cancellationToken);
}

