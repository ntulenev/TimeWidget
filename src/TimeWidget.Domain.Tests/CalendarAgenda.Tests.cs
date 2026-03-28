using FluentAssertions;

using TimeWidget.Domain.Calendar;

namespace TimeWidget.Domain.Tests;

public sealed class CalendarAgendaTests
{
    [Fact(DisplayName = "Empty should return agenda without events.")]
    [Trait("Category", "Unit")]
    public void EmptyShouldReturnAgendaWithoutEvents()
    {
        // Arrange
        var emptyAgenda = CalendarAgenda.Empty;

        // Act
        var events = emptyAgenda.Events;

        // Assert
        events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Constructor should use empty events when source is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldUseEmptyEventsWhenSourceIsNull()
    {
        // Arrange
        var agenda = new CalendarAgenda(null!);

        // Act
        // Assert
        agenda.Events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Constructor should expose provided events.")]
    [Trait("Category", "Unit")]
    public void CtorShouldExposeProvidedEvents()
    {
        // Arrange
        var calendarEvent = new CalendarEvent("Daily sync", DateTimeOffset.UtcNow, null, false, null);
        var events = new[] { calendarEvent };

        // Act
        var agenda = new CalendarAgenda(events);

        // Assert
        agenda.Events.Should().ContainSingle().Which.Should().BeSameAs(calendarEvent);
    }
}


