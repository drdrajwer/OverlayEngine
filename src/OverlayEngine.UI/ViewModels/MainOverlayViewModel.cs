using System.Reactive.Linq;
using OverlayEngine.Core.Platform;
using OverlayEngine.Core.Services;
using OverlayEngine.Core.Services.Network;
using OverlayEngine.UI.ViewModels.Widgets;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OverlayEngine.UI.ViewModels;

public sealed class MainOverlayViewModel : ViewModelBase, IDisposable
{
    [Reactive] public bool   IsEditMode    { get; set; }
    [Reactive] public bool   IsVisible     { get; set; } = true;
    [Reactive] public double Scale         { get; set; }
    [Reactive] public bool   ShowCpuGpu    { get; set; } = true;
    [Reactive] public bool   ShowAudio     { get; set; } = true;
    [Reactive] public bool   ShowNetwork   { get; set; } = true;
    [Reactive] public bool   ShowNotes     { get; set; } = true;
    [Reactive] public bool   ShowNowPlaying { get; set; } = true;
    [Reactive] public bool   ShowClock      { get; set; } = true;

    public CpuGpuWidgetViewModel      CpuGpu     { get; }
    public AudioWidgetViewModel       Audio      { get; }
    public NetworkWidgetViewModel     Network    { get; }
    public NotesWidgetViewModel       Notes      { get; }
    public NowPlayingWidgetViewModel  NowPlaying { get; }
    public ClockWidgetViewModel       Clock      { get; }

    private readonly ISettingsService _settings;

    public MainOverlayViewModel(ISettingsService settings)
    {
        _settings = settings;

        var cfg        = settings.Current;
        var sensors    = PlatformServiceFactory.CreateSensorService();
        var audio      = PlatformServiceFactory.CreateAudioService();
        var network    = new NetworkService();
        var nowPlaying = PlatformServiceFactory.CreateNowPlayingService();

        CpuGpu     = new CpuGpuWidgetViewModel(sensors,    cfg.SensorPollingIntervalMs, settings);
        Audio      = new AudioWidgetViewModel(audio,       cfg.UiRefreshIntervalMs);
        Network    = new NetworkWidgetViewModel(network,   cfg.SensorPollingIntervalMs);
        Notes      = new NotesWidgetViewModel(settings);
        NowPlaying = new NowPlayingWidgetViewModel(nowPlaying, cfg.SensorPollingIntervalMs);
        Clock      = new ClockWidgetViewModel();

        IsEditMode     = cfg.IsEditMode;
        Scale          = cfg.Scale;
        ShowCpuGpu     = cfg.ShowCpuGpuWidget;
        ShowAudio      = cfg.ShowAudioWidget;
        ShowNetwork    = cfg.ShowNetworkWidget;
        ShowNotes      = cfg.ShowNotesWidget;
        ShowNowPlaying = cfg.ShowNowPlayingWidget;
        ShowClock      = cfg.ShowClockWidget;

        this.WhenAnyValue(x => x.IsEditMode)
            .Subscribe(async val =>
            {
                settings.Current.IsEditMode = val;
                await settings.SaveAsync();
            });

        this.WhenAnyValue(x => x.Scale)
            .Skip(1)
            .Subscribe(async val =>
            {
                settings.Current.Scale = val;
                await settings.SaveAsync();
            });
    }

    public void ToggleVisibility() => IsVisible = !IsVisible;

    public void Dispose()
    {
        CpuGpu.Dispose();
        Audio.Dispose();
        Network.Dispose();
        NowPlaying.Dispose();
        Clock.Dispose();
    }
}
