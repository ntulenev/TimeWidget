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
        settings.ScalePercent.Should().BeNull();
        settings.ScreenPercent.Should().BeNull();
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
        var centerOffsetRatio = settings.GetCenterUpVerticalOffsetRatio();
        var idleOpacity = settings.GetIdleOpacity();
        var layoutScale = settings.GetLayoutScale(1.15);

        // Assert
        centerOffsetRatio.Should().Be(0.15);
        idleOpacity.Should().Be(0.75);
        layoutScale.Should().BeApproximately(1.38, 0.0001);
    }

    [Fact(DisplayName = "Screen percent should scale the widget relative to the target screen width.")]
    [Trait("Category", "Unit")]
    public void GetLayoutScaleForScreenShouldScaleWidgetRelativeToTargetScreenWidth()
    {
        // Arrange
        var settings = new WidgetPositioningSettings
        {
            ScreenPercent = 60
        };

        // Act
        var layoutScale = settings.GetLayoutScaleForScreen(
            1.15,
            780,
            1920);

        // Assert
        layoutScale.Should().BeApproximately(1.4769230769, 0.0001);
    }

    [Fact(DisplayName = "Scale percent should take precedence over screen percent.")]
    [Trait("Category", "Unit")]
    public void GetLayoutScaleForScreenShouldPreferScalePercentOverScreenPercent()
    {
        // Arrange
        var settings = new WidgetPositioningSettings
        {
            ScalePercent = 120,
            ScreenPercent = 60
        };

        // Act
        var layoutScale = settings.GetLayoutScaleForScreen(
            1.15,
            780,
            1920);

        // Assert
        layoutScale.Should().BeApproximately(1.38, 0.0001);
    }

    [Fact(DisplayName = "Screen-based scaling should use the default scale when no size option is configured.")]
    [Trait("Category", "Unit")]
    public void GetLayoutScaleForScreenShouldUseDefaultScaleWhenNoSizeOptionIsConfigured()
    {
        // Arrange
        var settings = new WidgetPositioningSettings();

        // Act
        var layoutScale = settings.GetLayoutScaleForScreen(
            1.15,
            780,
            1920);

        // Assert
        layoutScale.Should().Be(1.15);
    }

    [Theory(DisplayName = "Screen-based scaling should reject non-positive dimensions.")]
    [Trait("Category", "Unit")]
    [InlineData(0, 1920)]
    [InlineData(-1, 1920)]
    [InlineData(780, 0)]
    [InlineData(780, -1)]
    public void GetLayoutScaleForScreenShouldRejectNonPositiveDimensions(
        double widgetWidth,
        double screenWidth)
    {
        // Arrange
        var settings = new WidgetPositioningSettings
        {
            ScreenPercent = 60
        };

        // Act
        var action = () => settings.GetLayoutScaleForScreen(
            1.15,
            widgetWidth,
            screenWidth);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
