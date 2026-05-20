using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Avalonia.Media;
using OverlayEngine.Core.Services;
using OverlayEngine.Core.Services.Sensors;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OverlayEngine.UI.ViewModels.Widgets;

public sealed class CpuGpuWidgetViewModel : ViewModelBase, IDisposable
{
    [Reactive] public double CpuLoad    { get; private set; }
    [Reactive] public double CpuTemp    { get; private set; }
    [Reactive] public double GpuLoad    { get; private set; }
    [Reactive] public double GpuTemp    { get; private set; }
    [Reactive] public IBrush CpuGlow    { get; private set; } = Brushes.CornflowerBlue;
    [Reactive] public IBrush GpuGlow    { get; private set; } = Brushes.CornflowerBlue;

    private readonly ISettingsService _settings;
    private readonly IDisposable      _subscription;

    public CpuGpuWidgetViewModel(ISensorService sensorService, int pollIntervalMs, ISettingsService settings)
    {
        _settings = settings;

        _subscription = Observable
            .Interval(TimeSpan.FromMilliseconds(pollIntervalMs), NewThreadScheduler.Default)
            .SelectMany(_ => Observable.FromAsync(sensorService.ReadAsync))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(snap =>
            {
                CpuLoad = snap.CpuLoadPercent;
                CpuTemp = snap.CpuTempCelsius;
                GpuLoad = snap.GpuLoadPercent;
                GpuTemp = snap.GpuTempCelsius;
                CpuGlow = TempToGlow(snap.CpuTempCelsius);
                GpuGlow = TempToGlow(snap.GpuTempCelsius);
            });
    }

    private IBrush TempToGlow(double temp)
    {
        if (temp >= 80) return new SolidColorBrush(Color.Parse("#EF5350")); // hot red
        if (temp >= 75) return new SolidColorBrush(Color.Parse("#FFB74D")); // orange
        if (temp >= 60) return new SolidColorBrush(Color.Parse("#CE93D8")); // violet/warm

        // cool: use user's accent color
        var accent = _settings.Current.AccentColor;
        try   { return new SolidColorBrush(Color.Parse(accent)); }
        catch { return new SolidColorBrush(Color.Parse("#4FC3F7")); }
    }

    public void Dispose() => _subscription.Dispose();
}
