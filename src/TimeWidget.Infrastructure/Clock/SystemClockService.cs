using TimeWidget.Application.Abstractions;

namespace TimeWidget.Infrastructure.Clock;

public sealed class SystemClockService : IClockService
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

