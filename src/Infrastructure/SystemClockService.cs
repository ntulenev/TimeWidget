using TimeWidget.Abstractions;

namespace TimeWidget.Infrastructure;

public sealed class SystemClockService : IClockService
{
    public DateTime Now => DateTime.Now;
}

