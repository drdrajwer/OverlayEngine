using System.Globalization;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OverlayEngine.UI.ViewModels.Widgets;

public sealed class ClockWidgetViewModel : ViewModelBase, IDisposable
{
    [Reactive] public string TimeText { get; private set; } = "";
    [Reactive] public string DateText { get; private set; } = "";

    private readonly IDisposable _subscription;

    public ClockWidgetViewModel()
    {
        Tick();
        _subscription = Observable
            .Interval(TimeSpan.FromSeconds(1), RxApp.MainThreadScheduler)
            .Subscribe(_ => Tick());
    }

    private void Tick()
    {
        var now = DateTime.Now;
        TimeText = now.ToString("HH:mm:ss");
        DateText = now.ToString("ddd, d MMM", CultureInfo.CurrentCulture);
    }

    public void Dispose() => _subscription.Dispose();
}
