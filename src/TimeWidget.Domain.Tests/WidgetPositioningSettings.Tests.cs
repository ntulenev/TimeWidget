using FluentAssertions;

using TimeWidget.Domain.Configuration;

namespace TimeWidget.Domain.Tests;

public sealed class WidgetPositioningSettingsTests
{
    [Fact(DisplayName = "Constructor should initialize expected defaults.")]
    [Trait("Category", "Unit")]
    public void CtorShouldInitializeExpectedDefaults()
    {
        // Arrange
        var settings = new WidgetPositioningSettings();

        // Act
        // Assert
        settings.CenterUpVerticalOffsetPercent.Should().Be(15);
        settings.Opacity.Should().Be(75);
        settings.ScalePercent.Should().Be(100);
    }

    [Fact(DisplayName = "Methods should convert percentages to runtime values.")]
    [Trait("Category", "Unit")]
    public void MethodsShouldConvertPercentagesToRuntimeValues()
    {
        // Arrange
        var settings = new WidgetPositioningSettings
        {
            CenterUpVerticalOffsetPercent = 15,
            Opacity = 75,
            ScalePercent = 120
        };

        // Act
        // Assert
        settings.GetCenterUpVerticalOffsetRatio().Should().Be(0.15);
        settings.GetIdleOpacity().Should().Be(0.75);
        settings.GetLayoutScale(1.15).Should().BeApproximately(1.38, 0.0001);
    }
}


