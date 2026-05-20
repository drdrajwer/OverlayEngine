using System.Reactive.Concurrency;
using System.Reactive.Linq;
using OverlayEngine.Core.Services.NowPlaying;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OverlayEngine.UI.ViewModels.Widgets;

public sealed class NowPlayingWidgetViewModel : ViewModelBase, IDisposable
{
    [Reactive] public string Title     { get; private set; } = "—";
    [Reactive] public string Artist    { get; private set; } = "";
    [Reactive] public bool   IsPlaying { get; private set; }

    private readonly IDisposable _subscription;

    public NowPlayingWidgetViewModel(INowPlayingService service, int pollIntervalMs)
    {
        _subscription = Observable
            .Timer(TimeSpan.Zero,
                   TimeSpan.FromMilliseconds(Math.Max(pollIntervalMs, 3000)),
                   NewThreadScheduler.Default)
            .SelectMany(_ => Observable.FromAsync(service.ReadAsync))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(snap =>
            {
                IsPlaying = snap.IsPlaying;
                Title     = snap.IsPlaying && !string.IsNullOrEmpty(snap.Title)  ? snap.Title  : "—";
                Artist    = snap.IsPlaying && !string.IsNullOrEmpty(snap.Artist) ? snap.Artist : "";
            });
    }

    public void Dispose() => _subscription.Dispose();
}
