using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Win32;
using OverlayEngine.Core.Models;
using OverlayEngine.Core.Services;
using OverlayEngine.UI.Services;
using OverlayEngine.UI.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OverlayEngine.UI.ViewModels.ControlPanel;

public sealed class SettingsPanelViewModel : ViewModelBase
{
    // ── Visual ──────────────────────────────────────────────────────────
    [Reactive] public double BackgroundOpacity   { get; set; }
    [Reactive] public bool   EnableBlur          { get; set; }
    [Reactive] public double CornerRadius        { get; set; }
    [Reactive] public double Scale               { get; set; }

    // ── Colors ──────────────────────────────────────────────────────────
    [Reactive] public string AccentColor         { get; set; } = "#4FC3F7";
    [Reactive] public string TextColor           { get; set; } = "#FFFFFFFF";
    [Reactive] public string BorderColor         { get; set; } = "#33FFFFFF";
    [Reactive] public bool   ShowTileBorder      { get; set; }

    // ── Widget visibility ────────────────────────────────────────────────
    [Reactive] public bool ShowCpuGpuWidget      { get; set; } = true;
    [Reactive] public bool ShowAudioWidget       { get; set; } = true;
    [Reactive] public bool ShowNetworkWidget     { get; set; } = true;
    [Reactive] public bool ShowNotesWidget       { get; set; } = true;
    [Reactive] public bool ShowNowPlayingWidget  { get; set; } = true;
    [Reactive] public bool ShowClockWidget       { get; set; } = true;

    // ── Save feedback ───────────────────────────────────────────────────
    [Reactive] public string SaveButtonLabel     { get; set; } = "ZAPISZ";

    // ── Mode / layout ────────────────────────────────────────────────────
    [Reactive] public bool   IsEditMode          { get; set; }
    [Reactive] public bool   IsDynamicIslandMode { get; set; }
    [Reactive] public bool   IsHorizontalPill    { get; set; }
    [Reactive] public string FontFamily          { get; set; } = "Inter";
    [Reactive] public int    SensorPollMs        { get; set; }
    [Reactive] public int    UiRefreshMs         { get; set; }
    [Reactive] public string ToggleHotkey        { get; set; } = string.Empty;

    // ── Monitor ─────────────────────────────────────────────────────────
    [Reactive] public int    SelectedMonitorIndex { get; set; }
    public ObservableCollection<string> AvailableMonitors { get; } = [];

    // ── Theme ────────────────────────────────────────────────────────────
    [Reactive] public bool IsLightTheme { get; set; }

    // ── Autostart ───────────────────────────────────────────────────────
    [Reactive] public bool AutostartEnabled { get; set; }

    // ── Update ──────────────────────────────────────────────────────────
    [Reactive] public string UpdateStatus { get; set; } = "Kliknij aby sprawdzić";
    [Reactive] public bool   IsUpdating   { get; set; }

    // ── Commands ────────────────────────────────────────────────────────
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SaveCommand           { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ResetCommand          { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ExitCommand           { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> HideCommand           { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> MoveToMonitorCommand  { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CheckUpdatesCommand   { get; }
    public ReactiveCommand<string, System.Reactive.Unit>               SetAccentCommand      { get; }
    public ReactiveCommand<string, System.Reactive.Unit>               SetTextColorCommand   { get; }
    public ReactiveCommand<string, System.Reactive.Unit>               SetBorderColorCommand { get; }
    public ReactiveCommand<string, System.Reactive.Unit>               SetThemePresetCommand { get; }

    // Set by App.axaml.cs after window creation
    public Action<int>? MoveToMonitorAction { get; set; }

    private readonly ISettingsService     _settings;
    private readonly MainOverlayViewModel _overlay;

    // Linux autostart paths
    private static readonly string AutostartDir  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart");
    private static readonly string AutostartFile = Path.Combine(AutostartDir, "overlayengine.desktop");
    // Windows autostart registry key
    private const string WinRunKey   = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string WinAppName  = "OverlayEngine";

    public SettingsPanelViewModel(ISettingsService settings, MainOverlayViewModel overlay, Action exitAction, Action hideAction)
    {
        _settings = settings;
        _overlay  = overlay;

        LoadFromConfig(settings.Current);

        SaveCommand           = ReactiveCommand.CreateFromTask(SaveAsync);
        ResetCommand          = ReactiveCommand.CreateFromTask(ResetToDefaultsAsync);
        ExitCommand           = ReactiveCommand.Create(exitAction);
        HideCommand           = ReactiveCommand.Create(hideAction);
        MoveToMonitorCommand  = ReactiveCommand.Create(() => MoveToMonitorAction?.Invoke(SelectedMonitorIndex));
        CheckUpdatesCommand   = ReactiveCommand.CreateFromTask(CheckForUpdatesAsync,
            this.WhenAnyValue(x => x.IsUpdating).Select(v => !v));
        SetAccentCommand      = ReactiveCommand.Create<string>(c => AccentColor = c);
        SetTextColorCommand   = ReactiveCommand.Create<string>(c => TextColor = c);
        SetBorderColorCommand = ReactiveCommand.Create<string>(c => BorderColor = c);
        SetThemePresetCommand = ReactiveCommand.Create<string>(ApplyThemePreset);

        // Live-bind to overlay
        this.WhenAnyValue(x => x.IsEditMode)        .Subscribe(v => overlay.IsEditMode  = v);
        this.WhenAnyValue(x => x.Scale)             .Subscribe(v => overlay.Scale       = v);
        this.WhenAnyValue(x => x.ShowCpuGpuWidget)    .Subscribe(v => overlay.ShowCpuGpu     = v);
        this.WhenAnyValue(x => x.ShowAudioWidget)     .Subscribe(v => overlay.ShowAudio      = v);
        this.WhenAnyValue(x => x.ShowNetworkWidget)   .Subscribe(v => overlay.ShowNetwork    = v);
        this.WhenAnyValue(x => x.ShowNotesWidget)     .Subscribe(v => overlay.ShowNotes      = v);
        this.WhenAnyValue(x => x.ShowNowPlayingWidget).Subscribe(v => overlay.ShowNowPlaying = v);
        this.WhenAnyValue(x => x.ShowClockWidget)     .Subscribe(v => overlay.ShowClock      = v);

        // Live resource updates
        this.WhenAnyValue(
                x => x.AccentColor, x => x.TextColor, x => x.BorderColor,
                x => x.BackgroundOpacity, x => x.CornerRadius, x => x.ShowTileBorder)
            .Throttle(TimeSpan.FromMilliseconds(120))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ApplyResources());

        // Light theme live toggle
        this.WhenAnyValue(x => x.IsLightTheme)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ApplyResources());

        // Autostart live toggle
        this.WhenAnyValue(x => x.AutostartEnabled)
            .Skip(1)
            .Subscribe(SetAutostart);
    }

    private void LoadFromConfig(AppSettings cfg)
    {
        BackgroundOpacity   = cfg.BackgroundOpacity;
        EnableBlur          = cfg.EnableBlur;
        CornerRadius        = cfg.CornerRadius;
        Scale               = cfg.Scale;
        AccentColor         = cfg.AccentColor;
        TextColor           = cfg.TextColor;
        BorderColor         = cfg.BorderColor;
        ShowTileBorder      = cfg.ShowTileBorder;
        ShowCpuGpuWidget    = cfg.ShowCpuGpuWidget;
        ShowAudioWidget     = cfg.ShowAudioWidget;
        ShowNetworkWidget   = cfg.ShowNetworkWidget;
        ShowNotesWidget     = cfg.ShowNotesWidget;
        ShowNowPlayingWidget = cfg.ShowNowPlayingWidget;
        ShowClockWidget      = cfg.ShowClockWidget;
        IsEditMode          = _overlay.IsEditMode;
        IsLightTheme        = cfg.LightTheme;
        IsDynamicIslandMode = cfg.IsDynamicIslandMode;
        IsHorizontalPill    = cfg.IsHorizontalPill;
        FontFamily          = cfg.FontFamily;
        SensorPollMs        = cfg.SensorPollingIntervalMs;
        UiRefreshMs         = cfg.UiRefreshIntervalMs;
        ToggleHotkey        = cfg.ToggleHotkey;
        SelectedMonitorIndex = cfg.SelectedMonitorIndex;
        AutostartEnabled    = ReadAutostartEnabled();
    }

    private async Task SaveAsync()
    {
        var cfg = _settings.Current;
        cfg.BackgroundOpacity       = BackgroundOpacity;
        cfg.EnableBlur              = EnableBlur;
        cfg.CornerRadius            = CornerRadius;
        cfg.Scale                   = Scale;
        cfg.AccentColor             = AccentColor;
        cfg.TextColor               = TextColor;
        cfg.BorderColor             = BorderColor;
        cfg.ShowTileBorder          = ShowTileBorder;
        cfg.ShowCpuGpuWidget        = ShowCpuGpuWidget;
        cfg.ShowAudioWidget         = ShowAudioWidget;
        cfg.ShowNetworkWidget       = ShowNetworkWidget;
        cfg.ShowNotesWidget         = ShowNotesWidget;
        cfg.ShowNowPlayingWidget    = ShowNowPlayingWidget;
        cfg.ShowClockWidget         = ShowClockWidget;
        cfg.LightTheme              = IsLightTheme;
        cfg.IsDynamicIslandMode     = IsDynamicIslandMode;
        cfg.IsHorizontalPill        = IsHorizontalPill;
        cfg.FontFamily              = FontFamily;
        cfg.SensorPollingIntervalMs = SensorPollMs;
        cfg.UiRefreshIntervalMs     = UiRefreshMs;
        cfg.ToggleHotkey            = ToggleHotkey;
        cfg.SelectedMonitorIndex    = SelectedMonitorIndex;
        await _settings.SaveAsync();
        Dispatcher.UIThread.Post(ApplyResources);

        Dispatcher.UIThread.Post(() => SaveButtonLabel = "✓ ZAPISANO");
        await Task.Delay(2000);
        Dispatcher.UIThread.Post(() => SaveButtonLabel = "ZAPISZ");
    }

    private async Task ResetToDefaultsAsync()
    {
        var def = new AppSettings();
        LoadFromConfig(def);
        await SaveAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        IsUpdating   = true;
        UpdateStatus = "Sprawdzam aktualizacje...";
        try
        {
            using var svc       = new UpdateService();
            var       newVersion = await svc.CheckForUpdateAsync();
            if (newVersion is null)
            {
                UpdateStatus = $"✓ Masz najnowszą wersję (v{Program.AppVersion})";
                return;
            }
            UpdateStatus = $"Dostępna v{newVersion} — pobieranie...";
            await svc.DownloadAndInstallAsync(newVersion, p => UpdateStatus = $"Pobieranie: {p}%");
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Błąd: {ex.Message}";
        }
        finally
        {
            IsUpdating = false;
        }
    }

    internal void ApplyResources()
    {
        if (Application.Current?.Resources is not { } res) return;

        TrySetBrush(res, "AccentBrush", AccentColor);
        try
        {
            var ac = Color.Parse(AccentColor);
            res["AccentBrushFaint"] = new SolidColorBrush(new Color(30, ac.R, ac.G, ac.B));
        }
        catch { }
        res["TileCorner"]      = new CornerRadius(CornerRadius);
        res["TileBorderBrush"] = ShowTileBorder
            ? (Brush)new SolidColorBrush(TryParseColor(BorderColor, Color.Parse("#33FFFFFF")))
            : Brushes.Transparent;

        var tileAlpha = (byte)Math.Clamp(BackgroundOpacity * 255, 0, 255);

        if (IsLightTheme)
        {
            res["TileBrush"]              = new SolidColorBrush(new Color(tileAlpha, 240, 240, 240));
            TrySetBrush(res, "TextBrush", "#FF1A1A1A");

            res["LabelForeground"]        = new SolidColorBrush(Color.Parse("#FF888888"));
            res["UnitForeground"]         = new SolidColorBrush(Color.Parse("#FF999999"));
            res["IconBtnForeground"]      = new SolidColorBrush(Color.Parse("#FF666666"));
            res["InputBackground"]        = new SolidColorBrush(Color.Parse("#22000000"));
            res["InputBorder"]            = new SolidColorBrush(Color.Parse("#33000000"));
            res["InputForeground"]        = new SolidColorBrush(Color.Parse("#FF333333"));
            res["CheckboxForeground"]     = new SolidColorBrush(Color.Parse("#FF333333"));

            res["PanelBackground"]        = new SolidColorBrush(Color.Parse("#FFF5F5F5"));
            res["PanelTitleBackground"]   = new SolidColorBrush(Color.Parse("#FFEDEDED"));
            res["PanelBarBackground"]     = new SolidColorBrush(Color.Parse("#FFE8E8E8"));
            res["PanelTextForeground"]    = new SolidColorBrush(Color.Parse("#FF1A1A1A"));
            res["PanelSectionForeground"] = new SolidColorBrush(Color.Parse("#FF555555"));
            res["PanelSubtextForeground"] = new SolidColorBrush(Color.Parse("#FF888888"));
            res["PanelValueForeground"]   = new SolidColorBrush(Color.Parse("#FF666666"));
            res["PanelSepBackground"]     = new SolidColorBrush(Color.Parse("#22000000"));
            res["PanelInputBackground"]   = new SolidColorBrush(Color.Parse("#22000000"));
            res["PanelInputForeground"]   = new SolidColorBrush(Color.Parse("#FF333333"));
            res["PanelInputBorder"]       = new SolidColorBrush(Color.Parse("#33000000"));
        }
        else
        {
            res["TileBrush"]              = new SolidColorBrush(new Color(tileAlpha, 13, 13, 13));
            TrySetBrush(res, "TextBrush", TextColor);

            res["LabelForeground"]        = new SolidColorBrush(Color.Parse("#88FFFFFF"));
            res["UnitForeground"]         = new SolidColorBrush(Color.Parse("#66FFFFFF"));
            res["IconBtnForeground"]      = new SolidColorBrush(Color.Parse("#88FFFFFF"));
            res["InputBackground"]        = new SolidColorBrush(Color.Parse("#1AFFFFFF"));
            res["InputBorder"]            = new SolidColorBrush(Color.Parse("#33FFFFFF"));
            res["InputForeground"]        = new SolidColorBrush(Color.Parse("#FFFFFFFF"));
            res["CheckboxForeground"]     = new SolidColorBrush(Color.Parse("#CCFFFFFF"));

            res["PanelBackground"]        = new SolidColorBrush(Color.Parse("#FF0E0E0E"));
            res["PanelTitleBackground"]   = new SolidColorBrush(Color.Parse("#FF161616"));
            res["PanelBarBackground"]     = new SolidColorBrush(Color.Parse("#FF0A0A0A"));
            res["PanelTextForeground"]    = new SolidColorBrush(Color.Parse("#FFFFFFFF"));
            res["PanelSectionForeground"] = new SolidColorBrush(Color.Parse("#55FFFFFF"));
            res["PanelSubtextForeground"] = new SolidColorBrush(Color.Parse("#44FFFFFF"));
            res["PanelValueForeground"]   = new SolidColorBrush(Color.Parse("#77FFFFFF"));
            res["PanelSepBackground"]     = new SolidColorBrush(Color.Parse("#18FFFFFF"));
            res["PanelInputBackground"]   = new SolidColorBrush(Color.Parse("#1AFFFFFF"));
            res["PanelInputForeground"]   = new SolidColorBrush(Color.Parse("#CCFFFFFF"));
            res["PanelInputBorder"]       = new SolidColorBrush(Color.Parse("#33FFFFFF"));
        }
    }

    private static bool ReadAutostartEnabled()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(WinRunKey);
                return key?.GetValue(WinAppName) is not null;
            }
            catch { return false; }
        }
        return File.Exists(AutostartFile);
    }

    private static void SetAutostart(bool enable)
    {
        if (OperatingSystem.IsWindows())
        {
            SetAutostartWindows(enable);
            return;
        }
        SetAutostartLinux(enable);
    }

    [SupportedOSPlatform("windows")]
    private static void SetAutostartWindows(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(WinRunKey, writable: true);
            if (key is null) return;
            if (enable)
            {
                var exec = Environment.ProcessPath ?? "OverlayEngine.UI.exe";
                key.SetValue(WinAppName, $"\"{exec}\"");
            }
            else
            {
                key.DeleteValue(WinAppName, throwOnMissingValue: false);
            }
        }
        catch { /* silently ignore permission errors */ }
    }

    private static void SetAutostartLinux(bool enable)
    {
        try
        {
            if (enable)
            {
                Directory.CreateDirectory(AutostartDir);
                var exec = Environment.ProcessPath ?? "overlayengine";
                File.WriteAllText(AutostartFile,
                    $"""
                    [Desktop Entry]
                    Type=Application
                    Name=OverlayEngine
                    Comment=System overlay with CPU, GPU, mic, network and notes
                    Exec={exec}
                    Hidden=false
                    X-GNOME-Autostart-enabled=true
                    Terminal=false
                    """);
            }
            else
            {
                if (File.Exists(AutostartFile)) File.Delete(AutostartFile);
            }
        }
        catch { /* silently ignore permission errors */ }
    }

    // ── Theme presets ────────────────────────────────────────────────────
    private record ThemePreset(string Accent, string Text, bool Light,
                               string Border = "#33FFFFFF", double Opacity = 0.85);

    private static readonly Dictionary<string, ThemePreset> Presets = new()
    {
        ["CyberBlue"]  = new("#4FC3F7", "#FFFFFFFF", false),
        ["DarkOrange"] = new("#FF9800",  "#FFE0B2",   false),
        ["DarkGreen"]  = new("#4CAF50",  "#C8E6C9",   false),
        ["DarkPurple"] = new("#CE93D8",  "#E1BEE7",   false),
        ["Monochrome"] = new("#EEEEEE",  "#EEEEEE",   false, "#33FFFFFF"),
        ["Light"]      = new("#1976D2",  "#1A1A1A",   true,  "#22000000", 0.9),
    };

    private void ApplyThemePreset(string key)
    {
        if (!Presets.TryGetValue(key, out var p)) return;
        AccentColor       = p.Accent;
        TextColor         = p.Text;
        BorderColor       = p.Border;
        IsLightTheme      = p.Light;
        BackgroundOpacity = p.Opacity;
    }

    private static void TrySetBrush(Avalonia.Controls.IResourceDictionary res, string key, string hex)
    {
        try { res[key] = new SolidColorBrush(Color.Parse(hex)); }
        catch { }
    }

    private static Color TryParseColor(string hex, Color fallback)
    {
        try { return Color.Parse(hex); }
        catch { return fallback; }
    }
}
