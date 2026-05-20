using System.Reactive.Concurrency;
using System.Reactive.Linq;
using OverlayEngine.Core.Services.Audio;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OverlayEngine.UI.ViewModels.Widgets;

public sealed class AudioWidgetViewModel : ViewModelBase, IDisposable
{
    [Reactive] public double MicVolumeDb  { get; private set; } = -60;
    [Reactive] public bool   IsMuted      { get; private set; }
    [Reactive] public double MeterPercent { get; private set; }

    private readonly IDisposable _slowSub;
    private readonly IDisposable _fastSub;

    public AudioWidgetViewModel(IAudioService audioService, int pollIntervalMs)
    {
        // Slow poll: volume setting + mute state via pactl (max every 2 s to reduce process spawns)
        _slowSub = Observable
            .Timer(TimeSpan.Zero,
                   TimeSpan.FromMilliseconds(Math.Max(pollIntervalMs, 2000)),
                   NewThreadScheduler.Default)
            .SelectMany(_ => Observable.FromAsync(audioService.ReadAsync))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(snap =>
            {
                MicVolumeDb = snap.MicVolumeDb;
                IsMuted     = snap.IsMuted;
            });

        // Fast peak updates — 80 ms for smooth VU meter animation
        _fastSub = Observable
            .Interval(TimeSpan.FromMilliseconds(80), NewThreadScheduler.Default)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                if (IsMuted) { MeterPercent = 0; return; }
                var peak = audioService.PeakLevel;
                MeterPercent = peak >= 0
                    ? Math.Clamp(peak * 100.0, 0, 100)          // real amplitude from parec
                    : Math.Clamp((MicVolumeDb + 60.0) / 60.0 * 100.0, 0, 100); // fallback: vol setting
            });
    }

    public void Dispose()
    {
        _slowSub.Dispose();
        _fastSub.Dispose();
    }
}
