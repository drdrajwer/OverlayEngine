using System.Text.Json;
using OverlayEngine.Core.Models;

namespace OverlayEngine.Core.Services;

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "overlayengine", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public AppSettings Current { get; private set; } = new();

    public async Task LoadAsync()
    {
        if (!File.Exists(ConfigPath))
        {
            Current = new AppSettings();
            return;
        }
        await using var stream = File.OpenRead(ConfigPath);
        Current = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions)
                  ?? new AppSettings();
        // Migration: ensure required layout entries exist
        if (!Current.Widgets.Any(w => w.Id == "island"))
            Current.Widgets.Add(new WidgetLayout { Id = "island", X = 20, Y = 20, IsVisible = true });
        if (!Current.Widgets.Any(w => w.Id == "now_playing"))
        {
            var islandIdx = Current.Widgets.FindIndex(w => w.Id == "island");
            var entry = new WidgetLayout { Id = "now_playing", X = 20, Y = 580, IsVisible = true };
            if (islandIdx >= 0) Current.Widgets.Insert(islandIdx, entry);
            else Current.Widgets.Add(entry);
        }
        if (!Current.Widgets.Any(w => w.Id == "clock"))
        {
            // Insert before "island" so widget window index 5 maps to clock
            var islandIdx = Current.Widgets.FindIndex(w => w.Id == "island");
            var entry = new WidgetLayout { Id = "clock", X = 20, Y = 700, IsVisible = true };
            if (islandIdx >= 0) Current.Widgets.Insert(islandIdx, entry);
            else Current.Widgets.Add(entry);
        }
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        await using var stream = File.Create(ConfigPath);
        await JsonSerializer.SerializeAsync(stream, Current, JsonOptions);
    }
}
