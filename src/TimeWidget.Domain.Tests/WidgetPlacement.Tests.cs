using FluentAssertions;

using TimeWidget.Domain.Widget;

namespace TimeWidget.Domain.Tests;

public sealed class WidgetPlacementTests
{
    [Fact(DisplayName = "Is Pixel Unit should ignore case.")]
    [Trait("Category", "Unit")]
    public void IsPixelUnitShouldIgnoreCase()
    {
        // Arrange
        var placement = new WidgetPlacement(10, 20, "PX");

        // Act
        // Assert
        placement.IsPixelUnit.Should().BeTrue();
    }

    [Fact(DisplayName = "With Unit should trim value and fallback to pixels.")]
    [Trait("Category", "Unit")]
    public void WithUnitShouldTrimValueAndFallbackToPixels()
    {
        // Arrange
        var placement = new WidgetPlacement(10, 20, "dip");

        // Act
        // Assert
        placement.WithUnit(" rem ").Unit.Should().Be("rem");
        placement.WithUnit(" ").Unit.Should().Be(WidgetPlacement.PixelUnit);
    }
}

