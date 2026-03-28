namespace TimeWidget.Domain.Location;

/// <summary>
/// Represents geographic coordinates used by the widget.
/// </summary>
/// <param name="Latitude">The latitude.</param>
/// <param name="Longitude">The longitude.</param>
/// <param name="FallbackLabel">The fallback location label to use when reverse geocoding is unavailable.</param>
public sealed record Coordinates(double Latitude, double Longitude, string? FallbackLabel);
