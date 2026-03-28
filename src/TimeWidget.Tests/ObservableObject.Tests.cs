using FluentAssertions;

using TimeWidget.ViewModels;

namespace TimeWidget.Tests;

public sealed class ObservableObjectTests
{
    [Fact(DisplayName = "Set Property should update value and raise property changed.")]
    [Trait("Category", "Unit")]
    public void SetPropertyShouldUpdateValueAndRaisePropertyChanged()
    {
        // Arrange
        var observable = new TestObservableObject();
        var changedProperties = new List<string?>();
        observable.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        // Act
        var changed = observable.SetValue("updated");

        // Assert
        changed.Should().BeTrue();
        observable.Value.Should().Be("updated");
        changedProperties.Should().ContainSingle().Which.Should().Be(nameof(TestObservableObject.Value));
    }

    [Fact(DisplayName = "Set Property should return false when value is unchanged.")]
    [Trait("Category", "Unit")]
    public void SetPropertyShouldReturnFalseWhenValueIsUnchanged()
    {
        // Arrange
        var observable = new TestObservableObject();
        var eventRaised = false;
        observable.PropertyChanged += (_, _) => eventRaised = true;

        // Act
        var changed = observable.SetValue(string.Empty);

        // Assert
        changed.Should().BeFalse();
        eventRaised.Should().BeFalse();
    }

    private sealed class TestObservableObject : ObservableObject
    {
        private string _value = string.Empty;

        public string Value => _value;

        public bool SetValue(string value) => SetProperty(ref _value, value, nameof(Value));
    }
}

