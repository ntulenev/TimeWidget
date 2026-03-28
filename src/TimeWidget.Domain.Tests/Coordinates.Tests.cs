using FluentAssertions;

using TimeWidget.Domain.Location;

namespace TimeWidget.Domain.Tests;

public sealed class CoordinatesTests
{
    [Fact(DisplayName = "Constructor should expose assigned values.")]
    [Trait("Category", "Unit")]
    public void CtorShouldExposeAssignedValues()
    {
        // Arrange
        var coordinates = new Coordinates(52.52, 13.40, "Berlin");

        // Act
        // Assert
        coordinates.Latitude.Should().Be(52.52);
        coordinates.Longitude.Should().Be(13.40);
        coordinates.FallbackLabel.Should().Be("Berlin");
    }
}


