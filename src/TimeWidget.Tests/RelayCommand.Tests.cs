using FluentAssertions;

using TimeWidget.ViewModels;

namespace TimeWidget.Tests;

public sealed class RelayCommandTests
{
    [Fact(DisplayName = "Constructor should throw when execute is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenExecuteIsNull()
    {
        // Arrange
        var action = () => new RelayCommand(null!);

        // Act
        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Execute should invoke action.")]
    [Trait("Category", "Unit")]
    public void ExecuteShouldInvokeAction()
    {
        // Arrange
        var calls = 0;
        var command = new RelayCommand(() => calls++);

        // Act
        command.Execute(null);

        // Assert
        calls.Should().Be(1);
    }

    [Fact(DisplayName = "Can Execute should use delegate.")]
    [Trait("Category", "Unit")]
    public void CanExecuteShouldUseDelegate()
    {
        // Arrange
        var command = new RelayCommand(() => { }, () => false);

        // Act
        // Assert
        command.CanExecute(null).Should().BeFalse();
    }

    [Fact(DisplayName = "Notify Can Execute Changed should raise event.")]
    [Trait("Category", "Unit")]
    public void NotifyCanExecuteChangedShouldRaiseEvent()
    {
        // Arrange
        var command = new RelayCommand(() => { });
        var raised = 0;
        command.CanExecuteChanged += (_, _) => raised++;

        // Act
        command.NotifyCanExecuteChanged();

        // Assert
        raised.Should().Be(1);
    }
}


