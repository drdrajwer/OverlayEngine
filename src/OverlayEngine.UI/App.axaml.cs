using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using OverlayEngine.Core.Services;
using OverlayEngine.UI.ViewModels;
using OverlayEngine.UI.ViewModels.ControlPanel;
using OverlayEngine.UI.Views;
using OverlayEngine.UI.Views.Widgets;
using ClockWidget = OverlayEngine.UI.Views.Widgets.ClockWidget;
using ReactiveUI;

namespace OverlayEngine.UI;

public partial class App : Application
{
    private static void L(string msg)
        => File.AppendAllText(Program.LogPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n");

    private ControlPanelWindow?  _settingsWindow;
    private WidgetWindow[]?      _widgetWindows;
    private DynamicIslandWindow? _islandWindow;
    private PillWindow?          _pillWindow;
    private MainOverlayViewModel? _overlayVm;

    public override void Initialize()
    {
        L("Initialize()");
        AvaloniaXamlLoader.Load(this);
        L("Initialize() done");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        L("OnFrameworkInitializationCompleted()");
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            L("  No desktop lifetime");
            return;
        }

        try
        {
            var settings = new JsonSettingsService();
            Task.Run(settings.LoadAsync).GetAwaiter().GetResult();
            L($"  Settings loaded ({settings.Current.Widgets.Count} widgets)");

            _overlayVm = new MainOverlayViewModel(settings);
            L("  MainOverlayViewModel OK");

            Action exitAction = () =>
            {
                try { _overlayVm?.Dispose(); } catch { }
                Environment.Exit(0);
            };
            Action hideAction = () => _settingsWindow?.Hide();

            var settingsVm = new SettingsPanelViewModel(settings, _overlayVm, exitAction, hideAction);
            settingsVm.ApplyResources();

            _settingsWindow = new ControlPanelWindow(settingsVm);

            // 5 individual widget windows (indices match DefaultLayouts order: 0-4)
            _widgetWindows = new[]
            {
                new WidgetWindow(new CpuGpuWidget    { DataContext = _overlayVm.CpuGpu      }, _overlayVm, 0, settings),
                new WidgetWindow(new AudioWidget     { DataContext = _overlayVm.Audio       }, _overlayVm, 1, settings),
                new WidgetWindow(new NetworkWidget   { DataContext = _overlayVm.Network     }, _overlayVm, 2, settings),
                new WidgetWindow(new NotesWidget     { DataContext = _overlayVm.Notes       }, _overlayVm, 3, settings),
                new WidgetWindow(new NowPlayingWidget{ DataContext = _overlayVm.NowPlaying  }, _overlayVm, 4, settings),
                new WidgetWindow(new ClockWidget     { DataContext = _overlayVm.Clock       }, _overlayVm, 5, settings),
            };

            _islandWindow = new DynamicIslandWindow(_overlayVm, settings);
            _pillWindow   = new PillWindow(_overlayVm, settings);
            L("  All windows created");

            // Notes ⚙ → show/activate settings
            _overlayVm.Notes.ShowSettingsCommand.Subscribe(_ =>
            {
                if (_settingsWindow.IsVisible) _settingsWindow.Activate();
                else _settingsWindow.Show();
            });

            // ── Widget visibility (individual mode) ──
            _overlayVm.WhenAnyValue(x => x.ShowCpuGpu)    .Subscribe(v => SetWidgetVisible(_widgetWindows[0], v, settingsVm));
            _overlayVm.WhenAnyValue(x => x.ShowAudio)     .Subscribe(v => SetWidgetVisible(_widgetWindows[1], v, settingsVm));
            _overlayVm.WhenAnyValue(x => x.ShowNetwork)   .Subscribe(v => SetWidgetVisible(_widgetWindows[2], v, settingsVm));
            _overlayVm.WhenAnyValue(x => x.ShowNotes)     .Subscribe(v => SetWidgetVisible(_widgetWindows[3], v, settingsVm));
            _overlayVm.WhenAnyValue(x => x.ShowNowPlaying).Subscribe(v => SetWidgetVisible(_widgetWindows[4], v, settingsVm));
            _overlayVm.WhenAnyValue(x => x.ShowClock)     .Subscribe(v => SetWidgetVisible(_widgetWindows[5], v, settingsVm));

            // ── Mode switching (DynamicIsland / Pill / Individual) ──
            settingsVm.WhenAnyValue(x => x.IsDynamicIslandMode, x => x.IsHorizontalPill,
                                    (isDynamic, isPill) => (isDynamic, isPill))
                .Subscribe(t => ApplyDisplayMode(t.isDynamic, t.isPill, settingsVm, settings));

            // ── Monitor move action ──
            settingsVm.MoveToMonitorAction = idx => MoveToMonitor(idx, settingsVm, settings);

            // Settings window is MainWindow — keeps process alive; can only close via ExitCommand
            desktop.MainWindow = _settingsWindow;

            // Show initial mode
            ApplyDisplayMode(settings.Current.IsDynamicIslandMode, settings.Current.IsHorizontalPill, settingsVm, settings);
            _settingsWindow.Show();
            L("  All windows shown");

            RegisterToggleHotkey(settings.Current.ToggleHotkey, _overlayVm);
            base.OnFrameworkInitializationCompleted();
            L("  Startup complete");
        }
        catch (Exception ex)
        {
            L($"  EXCEPTION: {ex}");
        }
    }

    private void ApplyDisplayMode(bool isDynamic, bool isPill, SettingsPanelViewModel settingsVm, ISettingsService settings)
    {
        settings.Current.IsDynamicIslandMode = isDynamic;
        settings.Current.IsHorizontalPill    = isPill;

        if (!isDynamic)
        {
            // Individual windows
            _islandWindow!.Hide();
            _pillWindow!.Hide();
            for (int i = 0; i < _widgetWindows!.Length; i++)
                SetWidgetVisible(_widgetWindows[i], GetWidgetVisible(_overlayVm!, i), settingsVm);
        }
        else if (isPill)
        {
            // Horizontal pill
            foreach (var w in _widgetWindows!) w.Hide();
            _islandWindow!.Hide();
            if (!_pillWindow!.IsVisible) _pillWindow.Show();
        }
        else
        {
            // Vertical Dynamic Island
            foreach (var w in _widgetWindows!) w.Hide();
            _pillWindow!.Hide();
            if (!_islandWindow!.IsVisible) _islandWindow.Show();
        }

        Task.Run(settings.SaveAsync);
    }

    private static bool GetWidgetVisible(MainOverlayViewModel vm, int index) => index switch
    {
        0 => vm.ShowCpuGpu,
        1 => vm.ShowAudio,
        2 => vm.ShowNetwork,
        3 => vm.ShowNotes,
        4 => vm.ShowNowPlaying,
        5 => vm.ShowClock,
        _ => true,
    };

    private static void SetWidgetVisible(WidgetWindow window, bool visible, SettingsPanelViewModel settingsVm)
    {
        // Only apply in individual mode (not island/pill)
        if (settingsVm.IsDynamicIslandMode) return;
        if (visible && !window.IsVisible) window.Show();
        else if (!visible && window.IsVisible) window.Hide();
    }

    private void MoveToMonitor(int monitorIdx, SettingsPanelViewModel settingsVm, ISettingsService settings)
    {
        var screens = _settingsWindow!.Screens?.All?.ToList();
        if (screens is null || monitorIdx >= screens.Count) return;

        var workArea = screens[monitorIdx].WorkingArea;
        var ox = workArea.X;
        var oy = workArea.Y;

        // Move all individual windows to default positions on selected screen
        var offsets = new (int x, int y)[] { (20, 20), (20, 220), (20, 340), (20, 460), (20, 580), (20, 700) };
        for (int i = 0; i < _widgetWindows!.Length; i++)
        {
            _widgetWindows[i].Position = new PixelPoint(ox + offsets[i].x, oy + offsets[i].y);
            if (i < settings.Current.Widgets.Count)
            {
                settings.Current.Widgets[i].X = _widgetWindows[i].Position.X;
                settings.Current.Widgets[i].Y = _widgetWindows[i].Position.Y;
            }
        }

        // Move island/pill
        _islandWindow!.Position = new PixelPoint(ox + 60, oy + 60);
        _pillWindow!.Position   = new PixelPoint(ox + 100, oy + 20);

        var islandLayout = settings.Current.Widgets.FirstOrDefault(w => w.Id == "island");
        if (islandLayout is not null) { islandLayout.X = ox + 60; islandLayout.Y = oy + 60; }

        settings.Current.SelectedMonitorIndex = monitorIdx;
        Task.Run(settings.SaveAsync);
    }

    private void RegisterToggleHotkey(string hotkeyString, MainOverlayViewModel vm)
    {
        KeyGesture? gesture;
        try { gesture = KeyGesture.Parse(hotkeyString); }
        catch { return; }

        if (_settingsWindow is null) return;
        _settingsWindow.KeyDown += (_, e) =>
        {
            if (gesture.Matches(e)) vm.ToggleVisibility();
        };
    }
}
