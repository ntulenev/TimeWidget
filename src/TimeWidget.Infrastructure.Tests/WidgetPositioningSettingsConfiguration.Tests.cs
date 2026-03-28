using FluentAssertions;

using Microsoft.Extensions.Options;

using TimeWidget.Domain.Configuration;
using TimeWidget.Infrastructure.Configuration;

namespace TimeWidget.Infrastructure.Tests;

public sealed class WidgetPositioningSettingsConfigurationTests
{
    [Fact(DisplayName = "Post Configure should throw when options is null.")]
    [Trait("Category", "Unit")]
    public void PostConfigureShouldThrowWhenOptionsIsNull()
    {
        // Arrange
        var configuration = new WidgetPositioningSettingsConfiguration();

        // Act
        var action = () => configuration.PostConfigure(Options.DefaultName, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Post Configure should clamp values to supported range.")]
    [Trait("Category", "Unit")]
    public void PostConfigureShouldClampValuesToSupportedRange()
    {
        // Arrange
        var configuration = new WidgetPositioningSettingsConfiguration();
        var options = new WidgetPositioningSettings
        {
            CenterUpVerticalOffsetPercent = -5,
            Opacity = 120,
            ScalePercent = 20
        };

        // Act
        configuration.PostConfigure(Options.DefaultName, options);

        // Assert
        options.CenterUpVerticalOffsetPercent.Should().Be(0);
        options.Opacity.Should().Be(100);
        options.ScalePercent.Should().Be(50);
    }

    [Fact(DisplayName = "Validate should fail when scale percent is not positive.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldFailWhenScalePercentIsNotPositive()
    {
        // Arrange
        var configuration = new WidgetPositioningSettingsConfiguration();
        var options = new WidgetPositioningSettings
        {
            ScalePercent = 0
        };

        // Act
        var result = configuration.Validate(Options.DefaultName, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Be("WidgetPositioning:ScalePercent must be greater than zero.");
    }

    [Fact(DisplayName = "Validate should throw when options is null.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldThrowWhenOptionsIsNull()
    {
        // Arrange
        var configuration = new WidgetPositioningSettingsConfiguration();

        // Act
        var action = () => configuration.Validate(Options.DefaultName, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Validate should succeed when scale percent is positive.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldSucceedWhenScalePercentIsPositive()
    {
        // Arrange
        var configuration = new WidgetPositioningSettingsConfiguration();
        var options = new WidgetPositioningSettings
        {
            ScalePercent = 100
        };

        // Act
        var result = configuration.Validate(Options.DefaultName, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }
}

