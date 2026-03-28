using FluentAssertions;

using TimeWidget.Application.Clock;
using TimeWidget.ViewModels;

namespace TimeWidget.Tests;

public sealed class CityClockItemViewModelTests
{
    [Fact(DisplayName = "Apply should throw when state is null.")]
    [Trait("Category", "Unit")]
    public void ApplyShouldThrowWhenStateIsNull()
    {
        // Arrange
        var viewModel = new CityClockItemViewModel();

        // Act
        var action = () => viewModel.Apply(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Apply should update properties.")]
    [Trait("Category", "Unit")]
    public void ApplyShouldUpdateProperties()
    {
        // Arrange
        var viewModel = new CityClockItemViewModel();

        // Act
        viewModel.Apply(new CityClockDisplayState("Berlin", "09:15"));

        // Assert
        viewModel.Name.Should().Be("Berlin");
        viewModel.TimeText.Should().Be("09:15");
    }
}

