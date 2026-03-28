using FluentAssertions;

using TimeWidget.Application.Calendar;
using TimeWidget.Domain.Calendar;

namespace TimeWidget.Application.Tests;

public sealed class CalendarLoadResultTests
{
    [Fact(DisplayName = "Success should expose provided agenda.")]
    [Trait("Category", "Unit")]
    public void SuccessShouldExposeProvidedAgenda()
    {
        // Arrange
        var agenda = new CalendarAgenda(
        [
            new CalendarEvent("Planning", DateTimeOffset.UtcNow, null, false, null)
        ]);

        // Act
        var result = CalendarLoadResult.Success(agenda);

        // Assert
        result.Status.Should().Be(CalendarLoadStatus.Success);
        result.Agenda.Should().BeSameAs(agenda);
    }

    [Fact(DisplayName = "From Status should use empty agenda.")]
    [Trait("Category", "Unit")]
    public void FromStatusShouldUseEmptyAgenda()
    {
        // Arrange
        var result = CalendarLoadResult.FromStatus(CalendarLoadStatus.NoUpcomingEvents);

        // Act
        // Assert
        result.Status.Should().Be(CalendarLoadStatus.NoUpcomingEvents);
        result.Agenda.Events.Should().BeEmpty();
    }
}

