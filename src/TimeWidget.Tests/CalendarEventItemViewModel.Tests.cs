using FluentAssertions;

using TimeWidget.ViewModels;

namespace TimeWidget.Tests;

public sealed class CalendarEventItemViewModelTests
{
    [Fact(DisplayName = "Constructor should set properties.")]
    [Trait("Category", "Unit")]
    public void CtorShouldSetProperties()
    {
        // Arrange
        var viewModel = new CalendarEventItemViewModel("Standup", "Today - 09:00", "M");

        // Act
        // Assert
        viewModel.Title.Should().Be("Standup");
        viewModel.ScheduleText.Should().Be("Today - 09:00");
        viewModel.ResponseSymbol.Should().Be("M");
    }
}


