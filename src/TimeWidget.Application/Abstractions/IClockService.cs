namespace TimeWidget.Application.Abstractions;

public interface IClockService
{
    DateTimeOffset Now { get; }
}
