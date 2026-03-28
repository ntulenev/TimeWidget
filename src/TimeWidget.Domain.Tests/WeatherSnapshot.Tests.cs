using FluentAssertions;

using TimeWidget.Domain.Weather;

namespace TimeWidget.Domain.Tests;

public sealed class WeatherSnapshotTests
{
    [Fact(DisplayName = "Constructor should expose assigned values.")]
    [Trait("Category", "Unit")]
    public void CtorShouldExposeAssignedValues()
    {
        // Arrange
        var snapshot = new WeatherSnapshot(21, "Clear", "Berlin");

        // Act
        // Assert
        snapshot.Temperature.Should().Be(21);
        snapshot.Condition.Should().Be("Clear");
        snapshot.Location.Should().Be("Berlin");
    }
}


