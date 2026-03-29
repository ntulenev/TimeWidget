using Windows.Devices.Geolocation;

namespace TimeWidget.Infrastructure.Location;

internal interface IWindowsLocationApi
{
    Task<GeolocationAccessStatus> RequestAccessAsync();

    Task<BasicGeoposition> GetPositionAsync(TimeSpan maximumAge, TimeSpan timeout);
}
