using TimeWidget.Application.Abstractions;
using TimeWidget.Domain.Location;
using Windows.Devices.Geolocation;

namespace TimeWidget.Infrastructure.Location;

public sealed class WindowsLocationService : ILocationService
{
    private static readonly double? FallbackLatitude = null;
    private static readonly double? FallbackLongitude = null;
    private static readonly string? FallbackLocationLabel = null;

    public async Task<Coordinates?> TryGetCoordinatesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            if (accessStatus != GeolocationAccessStatus.Allowed)
            {
                return GetFallbackCoordinates();
            }

            var geolocator = new Geolocator
            {
                DesiredAccuracyInMeters = 5000
            };

            var position = await geolocator.GetGeopositionAsync(
                maximumAge: TimeSpan.FromMinutes(30),
                timeout: TimeSpan.FromSeconds(10));

            cancellationToken.ThrowIfCancellationRequested();

            var point = position.Coordinate.Point.Position;
            return new Coordinates(point.Latitude, point.Longitude, null);
        }
        catch
        {
            return GetFallbackCoordinates();
        }
    }

    private static Coordinates? GetFallbackCoordinates()
    {
        return FallbackLatitude.HasValue && FallbackLongitude.HasValue
            ? new Coordinates(
                FallbackLatitude.Value,
                FallbackLongitude.Value,
                FallbackLocationLabel)
            : null;
    }
}

