using TimeWidget.Domain.Location;

namespace TimeWidget.Application.Abstractions;

public interface ILocationService
{
    Task<Coordinates?> TryGetCoordinatesAsync(CancellationToken cancellationToken);
}
