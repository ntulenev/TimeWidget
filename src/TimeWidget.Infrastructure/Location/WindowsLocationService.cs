using TimeWidget.Application.Abstractions;
using TimeWidget.Domain.Location;

using Windows.Devices.Geolocation;

namespace TimeWidget.Infrastructure.Location;

/// <summary>
/// Resolves the current location using Windows geolocation APIs.
/// </summary>
public sealed class WindowsLocationService : ILocationService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsLocationService"/> class.
    /// </summary>
    public WindowsLocationService()
        : this(new WindowsLocationApi())
    {
    }

    internal WindowsLocationService(IWindowsLocationApi locationApi)
    {
        ArgumentNullException.ThrowIfNull(locationApi);

        _locationApi = locationApi;
    }

    /// <inheritdoc />
    public async Task<Coordinates?> TryGetCoordinatesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var accessStatus = await _locationApi.RequestAccessAsync();
            if (accessStatus != GeolocationAccessStatus.Allowed)
            {
                return GetFallbackCoordinates();
            }

            var position = await _locationApi.GetPositionAsync(
                maximumAge: TimeSpan.FromMinutes(30),
                timeout: TimeSpan.FromSeconds(10));

            cancellationToken.ThrowIfCancellationRequested();
            return new Coordinates(position.Latitude, position.Longitude, null);
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

    private static double? FallbackLatitude => null;
    private static double? FallbackLongitude => null;
    private static string? FallbackLocationLabel => null;

    private readonly IWindowsLocationApi _locationApi;
}

