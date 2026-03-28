namespace TimeWidget.Application.Abstractions;

/// <summary>
/// Provides the current time for the widget.
/// </summary>
public interface IClockService
{
    /// <summary>
    /// Gets the current timestamp.
    /// </summary>
    DateTimeOffset Now { get; }
}
