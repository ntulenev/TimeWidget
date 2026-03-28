using FluentAssertions;

using TimeWidget.Application.Weather;
using TimeWidget.Domain.Weather;

namespace TimeWidget.Application.Tests;

public sealed class WeatherLoadResultTests
{
    [Fact(DisplayName = "Success should expose provided snapshot.")]
    [Trait("Category", "Unit")]
    public void SuccessShouldExposeProvidedSnapshot()
    {
        // Arrange
        var snapshot = new WeatherSnapshot(21, "Clear", "Berlin");

        // Act
        var result = WeatherLoadResult.Success(snapshot);

        // Assert
        result.Status.Should().Be(WeatherLoadStatus.Success);
        result.Snapshot.Should().BeSameAs(snapshot);
    }

    [Fact(DisplayName = "From Status should use null snapshot.")]
    [Trait("Category", "Unit")]
    public void FromStatusShouldUseNullSnapshot()
    {
        // Arrange
        var result = WeatherLoadResult.FromStatus(WeatherLoadStatus.Unavailable);

        // Act
        // Assert
        result.Status.Should().Be(WeatherLoadStatus.Unavailable);
        result.Snapshot.Should().BeNull();
    }
}

