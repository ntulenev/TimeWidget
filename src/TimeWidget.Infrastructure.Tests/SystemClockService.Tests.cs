using FluentAssertions;

using TimeWidget.Infrastructure.Clock;

namespace TimeWidget.Infrastructure.Tests;

public sealed class SystemClockServiceTests
{
    [Fact(DisplayName = "Now should return current system time.")]
    [Trait("Category", "Unit")]
    public void NowShouldReturnCurrentSystemTime()
    {
        // Arrange
        var before = DateTimeOffset.Now;
        var service = new SystemClockService();

        // Act
        var now = service.Now;

        // Assert
        var after = DateTimeOffset.Now;
        now.Should().BeOnOrAfter(before);
        now.Should().BeOnOrBefore(after);
    }
}

