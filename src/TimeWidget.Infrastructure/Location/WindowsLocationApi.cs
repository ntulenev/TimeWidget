using Windows.Devices.Geolocation;

namespace TimeWidget.Infrastructure.Location;

internal sealed class WindowsLocationApi : IWindowsLocationApi
{
    public Task<GeolocationAccessStatus> RequestAccessAsync()
    {
        return Geolocator.RequestAccessAsync().AsTask();
    }

    public async Task<BasicGeoposition> GetPositionAsync(TimeSpan maximumAge, TimeSpan timeout)
    {
        var geolocator = new Geolocator
        {
            DesiredAccuracyInMeters = 5000
        };

        var position = await geolocator.GetGeopositionAsync(maximumAge, timeout);
        return position.Coordinate.Point.Position;
    }
}
