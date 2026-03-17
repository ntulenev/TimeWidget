using TimeWidget.Abstractions;

namespace TimeWidget.Infrastructure;

public sealed class SystemClockService : IClockService
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

