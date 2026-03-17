namespace TimeWidget.Abstractions;

public interface IClockService
{
    DateTimeOffset Now { get; }
}

