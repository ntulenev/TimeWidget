namespace TimeWidget.Models;

public readonly record struct WidgetPlacementSettings(
    double Left,
    double Top,
    string? Unit = null);

