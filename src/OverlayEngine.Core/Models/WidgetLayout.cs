namespace OverlayEngine.Core.Models;

public sealed class WidgetLayout
{
    public string Id { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsVisible { get; set; } = true;
}
