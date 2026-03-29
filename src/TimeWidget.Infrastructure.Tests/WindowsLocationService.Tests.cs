using FluentAssertions;

using TimeWidget.Infrastructure.Location;

using Windows.Devices.Geolocation;

namespace TimeWidget.Infrastructure.Tests;

public sealed class WindowsLocationServiceTests
{
    [Fact(DisplayName = "Try Get Coordinates should honor cancellation before calling Windows apis.")]
    [Trait("Category", "Unit")]
    public async Task TryGetCoordinatesAsyncShouldHonorCancellationBeforeCallingWindowsApis()
    {
        // Arrange
        var service = new WindowsLocationService();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var action = async () => await service.TryGetCoordinatesAsync(cancellationTokenSource.Token);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "Try Get Coordinates should return null when access is denied.")]
    [Trait("Category", "Unit")]
    public async Task TryGetCoordinatesAsyncShouldReturnNullWhenAccessIsDenied()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var locationApi = new StubWindowsLocationApi
        {
            AccessStatus = GeolocationAccessStatus.Denied
        };
        var service = new WindowsLocationService(locationApi);

        // Act
        var coordinates = await service.TryGetCoordinatesAsync(cancellationTokenSource.Token);

        // Assert
        coordinates.Should().BeNull();
        locationApi.RequestAccessCalls.Should().Be(1);
        locationApi.GetPositionCalls.Should().Be(0);
    }

    [Fact(DisplayName = "Try Get Coordinates should return coordinates when access is allowed.")]
    [Trait("Category", "Unit")]
    public async Task TryGetCoordinatesAsyncShouldReturnCoordinatesWhenAccessIsAllowed()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var locationApi = new StubWindowsLocationApi
        {
            AccessStatus = GeolocationAccessStatus.Allowed,
            Position = new BasicGeoposition
            {
                Latitude = 52.52,
                Longitude = 13.40
            }
        };
        var service = new WindowsLocationService(locationApi);

        // Act
        var coordinates = await service.TryGetCoordinatesAsync(cancellationTokenSource.Token);

        // Assert
        coordinates.Should().Be(new TimeWidget.Domain.Location.Coordinates(52.52, 13.40, null));
        locationApi.RequestAccessCalls.Should().Be(1);
        locationApi.GetPositionCalls.Should().Be(1);
        locationApi.LastMaximumAge.Should().Be(TimeSpan.FromMinutes(30));
        locationApi.LastTimeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact(DisplayName = "Try Get Coordinates should return null when Windows apis throw.")]
    [Trait("Category", "Unit")]
    public async Task TryGetCoordinatesAsyncShouldReturnNullWhenWindowsApisThrow()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var locationApi = new StubWindowsLocationApi
        {
            AccessStatus = GeolocationAccessStatus.Allowed,
            GetPositionException = new InvalidOperationException("boom")
        };
        var service = new WindowsLocationService(locationApi);

        // Act
        var coordinates = await service.TryGetCoordinatesAsync(cancellationTokenSource.Token);

        // Assert
        coordinates.Should().BeNull();
        locationApi.RequestAccessCalls.Should().Be(1);
        locationApi.GetPositionCalls.Should().Be(1);
    }

    private sealed class StubWindowsLocationApi : IWindowsLocationApi
    {
        public GeolocationAccessStatus AccessStatus { get; init; }

        public BasicGeoposition Position { get; init; }

        public Exception? GetPositionException { get; init; }

        public int RequestAccessCalls { get; private set; }

        public int GetPositionCalls { get; private set; }

        public TimeSpan? LastMaximumAge { get; private set; }

        public TimeSpan? LastTimeout { get; private set; }

        public Task<GeolocationAccessStatus> RequestAccessAsync()
        {
            RequestAccessCalls++;
            return Task.FromResult(AccessStatus);
        }

        public Task<BasicGeoposition> GetPositionAsync(TimeSpan maximumAge, TimeSpan timeout)
        {
            GetPositionCalls++;
            LastMaximumAge = maximumAge;
            LastTimeout = timeout;

            if (GetPositionException is not null)
            {
                throw GetPositionException;
            }

            return Task.FromResult(Position);
        }
    }
}

