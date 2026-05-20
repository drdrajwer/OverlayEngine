using System.Text.Json.Serialization;

namespace OverlayEngine.Core.Models;

public sealed class AppSettings
{
    public double BackgroundOpacity { get; set; } = 0.85;
    public bool EnableBlur { get; set; } = true;
    public double CornerRadius { get; set; } = 10.0;
    public string FontFamily { get; set; } = "Inter";
    public bool IsEditMode { get; set; } = false;
    public int SensorPollingIntervalMs { get; set; } = 1000;
    public int UiRefreshIntervalMs { get; set; } = 100;
    public string ToggleHotkey { get; set; } = "Ctrl+Shift+O";
    public string AccentColor          { get; set; } = "#4FC3F7";
    public string TextColor            { get; set; } = "#FFFFFFFF";
    public string BorderColor          { get; set; } = "#33FFFFFF";
    public bool   ShowTileBorder       { get; set; } = false;
    public bool   IsDynamicIslandMode  { get; set; } = false;
    public bool   IsHorizontalPill     { get; set; } = false;
    public bool   ShowCpuGpuWidget     { get; set; } = true;
    public bool   ShowAudioWidget      { get; set; } = true;
    public bool   ShowNetworkWidget    { get; set; } = true;
    public bool   ShowNotesWidget      { get; set; } = true;
    public bool   ShowNowPlayingWidget { get; set; } = true;
    public bool   ShowClockWidget      { get; set; } = true;
    public int    SelectedMonitorIndex { get; set; } = 0;
    public bool   AutostartEnabled     { get; set; } = false;
    public bool   LightTheme          { get; set; } = false;
    public double Scale                { get; set; } = 1.0;
    public List<WidgetLayout> Widgets { get; set; } = DefaultLayouts();
    public List<NoteItem> Notes { get; set; } = [];

    private static List<WidgetLayout> DefaultLayouts() =>
    [
        new() { Id = "cpu_gpu",     X = 20, Y = 20,  IsVisible = true },
        new() { Id = "audio",       X = 20, Y = 220, IsVisible = true },
        new() { Id = "network",     X = 20, Y = 340, IsVisible = true },
        new() { Id = "notes",       X = 20, Y = 460, IsVisible = true },
        new() { Id = "now_playing", X = 20, Y = 580, IsVisible = true },
        new() { Id = "clock",       X = 20, Y = 700, IsVisible = true },
        new() { Id = "island",      X = 20, Y = 20,  IsVisible = true },
    ];
}
