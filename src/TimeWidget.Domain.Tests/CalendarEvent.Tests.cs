using FluentAssertions;

using TimeWidget.Domain.Calendar;

namespace TimeWidget.Domain.Tests;

public sealed class CalendarEventTests
{
    [Fact(DisplayName = "Constructor should expose assigned values.")]
    [Trait("Category", "Unit")]
    public void CtorShouldExposeAssignedValues()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 3, 28, 9, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(1);

        // Act
        var calendarEvent = new CalendarEvent("Planning", start, end, true, "accepted");

        // Assert
        calendarEvent.Title.Should().Be("Planning");
        calendarEvent.Start.Should().Be(start);
        calendarEvent.End.Should().Be(end);
        calendarEvent.IsAllDay.Should().BeTrue();
        calendarEvent.SelfResponseStatus.Should().Be("accepted");
    }

    [Fact(DisplayName = "Safe Title should return title when value provided.")]
    [Trait("Category", "Unit")]
    public void SafeTitleShouldReturnTitleWhenValueProvided()
    {
        // Arrange
        var calendarEvent = new CalendarEvent("Planning", DateTimeOffset.UtcNow, null, false, null);

        // Act
        // Assert
        calendarEvent.SafeTitle.Should().Be("Planning");
    }

    [Theory(DisplayName = "Safe Title should return fallback when title is empty.")]
    [Trait("Category", "Unit")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void SafeTitleShouldReturnFallbackWhenTitleIsEmpty(string? title)
    {
        // Arrange
        var calendarEvent = new CalendarEvent(title!, DateTimeOffset.UtcNow, null, false, null);

        // Act
        // Assert
        calendarEvent.SafeTitle.Should().Be("Untitled event");
    }
}


