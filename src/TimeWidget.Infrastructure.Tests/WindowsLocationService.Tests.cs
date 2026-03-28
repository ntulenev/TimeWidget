using FluentAssertions;

using TimeWidget.Infrastructure.Location;

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
}

