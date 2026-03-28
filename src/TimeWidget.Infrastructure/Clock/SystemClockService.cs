using TimeWidget.Application.Abstractions;

namespace TimeWidget.Infrastructure.Clock;

/// <summary>
/// Provides the current system time.
/// </summary>
public sealed class SystemClockService : IClockService
{
    /// <inheritdoc />
    public DateTimeOffset Now => DateTimeOffset.Now;
}

