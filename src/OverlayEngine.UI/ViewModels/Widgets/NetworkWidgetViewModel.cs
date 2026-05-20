using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using OverlayEngine.Core.Services.Network;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OverlayEngine.UI.ViewModels.Widgets;

public sealed class NetworkWidgetViewModel : ViewModelBase, IDisposable
{
    private const int SparklinePoints = 30;

    [Reactive] public string DownloadLabel { get; private set; } = "↓ 0 B/s";
    [Reactive] public string UploadLabel   { get; private set; } = "↑ 0 B/s";

    public ObservableCollection<double> DownloadHistory { get; } = [];
    public ObservableCollection<double> UploadHistory   { get; } = [];

    private readonly IDisposable _subscription;

    public NetworkWidgetViewModel(INetworkService networkService, int pollIntervalMs)
    {
        for (var i = 0; i < SparklinePoints; i++) { DownloadHistory.Add(0); UploadHistory.Add(0); }

        _subscription = Observable
            .Interval(TimeSpan.FromMilliseconds(pollIntervalMs), NewThreadScheduler.Default)
            .SelectMany(_ => Observable.FromAsync(networkService.ReadAsync))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(snap =>
            {
                DownloadLabel = $"↓ {FormatBytes(snap.DownloadBytesPerSec)}/s";
                UploadLabel   = $"↑ {FormatBytes(snap.UploadBytesPerSec)}/s";

                PushSparkline(DownloadHistory, snap.DownloadBytesPerSec);
                PushSparkline(UploadHistory,   snap.UploadBytesPerSec);
            });
    }

    private static void PushSparkline(ObservableCollection<double> history, double value)
    {
        history.RemoveAt(0);
        history.Add(value);
    }

    private static string FormatBytes(double bps) => bps switch
    {
        >= 1_073_741_824 => $"{bps / 1_073_741_824:F1} GB",
        >= 1_048_576     => $"{bps / 1_048_576:F1} MB",
        >= 1_024         => $"{bps / 1_024:F1} KB",
        _                => $"{bps:F0} B",
    };

    public void Dispose() => _subscription.Dispose();
}
