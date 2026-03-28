using FluentAssertions;

using TimeWidget.Application.Calendar;
using TimeWidget.Domain.Calendar;
using TimeWidget.Domain.Configuration;

namespace TimeWidget.Application.Tests;

public sealed class CalendarDisplayBuilderTests
{
    [Fact(DisplayName = "Build should create compact state for tentative meeting.")]
    [Trait("Category", "Unit")]
    public void BuildShouldCreateCompactStateForTentativeMeeting()
    {
        // Arrange
        var builder = new CalendarDisplayBuilder();
        var now = CreateLocalDateTimeOffset(2026, 3, 28, 9, 0);
        var calendarEvent = new CalendarEvent(
            "Standup",
            now.AddHours(2),
            now.AddHours(3),
            false,
            "tentative");
        var result = CalendarLoadResult.Success(new CalendarAgenda([calendarEvent]));
        var settings = new GoogleCalendarSettings
        {
            Mode = GoogleCalendarMode.Compact
        };

        // Act
        var state = builder.Build(result, settings, now);

        // Assert
        state.Events.Should().ContainSingle();
        state.Events[0].Title.Should().Be("Standup");
        state.Events[0].ScheduleText.Should().Be($"Today - {calendarEvent.Start.ToLocalTime():HH:mm}");
        state.Events[0].ResponseSymbol.Should().Be("M");
        state.StatusText.Should().BeEmpty();
        state.ShowSection.Should().BeTrue();
        state.ShowEvents.Should().BeTrue();
        state.ShowStatus.Should().BeFalse();
        state.ShowCompactSection.Should().BeTrue();
        state.ShowFullSection.Should().BeFalse();
    }

    [Fact(DisplayName = "Build should use tomorrow label for all day event.")]
    [Trait("Category", "Unit")]
    public void BuildShouldUseTomorrowLabelForAllDayEvent()
    {
        // Arrange
        var builder = new CalendarDisplayBuilder();
        var now = CreateLocalDateTimeOffset(2026, 3, 28, 9, 0);
        var calendarEvent = new CalendarEvent(
            "Planning",
            CreateLocalDateTimeOffset(2026, 3, 29, 0, 0),
            CreateLocalDateTimeOffset(2026, 3, 30, 0, 0),
            true,
            null);
        var result = CalendarLoadResult.Success(new CalendarAgenda([calendarEvent]));
        var settings = new GoogleCalendarSettings();

        // Act
        var state = builder.Build(result, settings, now);

        // Assert
        state.Events.Should().ContainSingle();
        state.Events[0].ScheduleText.Should().Be("Tomorrow - All day");
        state.Events[0].ResponseSymbol.Should().Be("?");
    }

    [Fact(DisplayName = "Building the signed-out state should show full calendar status.")]
    [Trait("Category", "Unit")]
    public void BuildSignedOutStateShouldShowFullCalendarStatus()
    {
        // Arrange
        var builder = new CalendarDisplayBuilder();
        var settings = new GoogleCalendarSettings
        {
            Mode = GoogleCalendarMode.FullCalendar
        };

        // Act
        var state = builder.BuildSignedOutState(settings);

        // Assert
        state.Events.Should().BeEmpty();
        state.StatusText.Should().Be("Google Calendar sign-in removed");
        state.ShowSection.Should().BeTrue();
        state.ShowEvents.Should().BeFalse();
        state.ShowStatus.Should().BeTrue();
        state.ShowCompactSection.Should().BeFalse();
        state.ShowFullSection.Should().BeTrue();
    }

    [Fact(DisplayName = "Build should show status without events for no upcoming events.")]
    [Trait("Category", "Unit")]
    public void BuildShouldShowStatusWithoutEventsForNoUpcomingEvents()
    {
        // Arrange
        var builder = new CalendarDisplayBuilder();
        var settings = new GoogleCalendarSettings
        {
            Mode = GoogleCalendarMode.Compact
        };

        // Act
        var state = builder.Build(
            CalendarLoadResult.FromStatus(CalendarLoadStatus.NoUpcomingEvents),
            settings,
            CreateLocalDateTimeOffset(2026, 3, 28, 9, 0));

        // Assert
        state.Events.Should().BeEmpty();
        state.StatusText.Should().Be("No upcoming meetings");
        state.ShowSection.Should().BeTrue();
        state.ShowEvents.Should().BeFalse();
        state.ShowStatus.Should().BeTrue();
        state.ShowCompactSection.Should().BeTrue();
        state.ShowFullSection.Should().BeFalse();
    }

    [Fact(DisplayName = "Build should use formatted date and declined response for future meeting.")]
    [Trait("Category", "Unit")]
    public void BuildShouldUseFormattedDateAndDeclinedResponseForFutureMeeting()
    {
        // Arrange
        var builder = new CalendarDisplayBuilder();
        var now = CreateLocalDateTimeOffset(2026, 3, 28, 9, 0);
        var calendarEvent = new CalendarEvent(
            "Review",
            CreateLocalDateTimeOffset(2026, 4, 2, 14, 30),
            CreateLocalDateTimeOffset(2026, 4, 2, 15, 30),
            false,
            "declined");

        // Act
        var state = builder.Build(
            CalendarLoadResult.Success(new CalendarAgenda([calendarEvent])),
            new GoogleCalendarSettings(),
            now);

        // Assert
        state.Events.Should().ContainSingle();
        state.Events[0].ScheduleText.Should().Be("Thu, 2 Apr - 14:30");
        state.Events[0].ResponseSymbol.Should().Be("N");
    }

    private static DateTimeOffset CreateLocalDateTimeOffset(int year, int month, int day, int hour, int minute)
    {
        var localDateTime = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Local);
        return new DateTimeOffset(localDateTime);
    }
}


