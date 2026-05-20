using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using OverlayEngine.UI.ViewModels.Widgets;
using ReactiveUI;

namespace OverlayEngine.UI.Views.Widgets;

public partial class AudioWidget : UserControl, IViewFor<AudioWidgetViewModel>
{
    public AudioWidgetViewModel? ViewModel { get; set; }
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (AudioWidgetViewModel?)value;
    }

    private double _barMaxWidth = 120;

    public AudioWidget()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            if (DataContext is not AudioWidgetViewModel vm) return;

            d.Add(vm.WhenAnyValue(x => x.MeterPercent, x => x.IsMuted, x => x.MicVolumeDb)
                    .Subscribe(t => UpdateUi(t.Item1, t.Item2, t.Item3)));
        });
    }

    private void UpdateUi(double pct, bool muted, double db)
    {
        // VU bar
        if (this.FindControl<Border>("VuBar") is { } bar)
        {
            var parent = bar.Parent as Border;
            _barMaxWidth = parent?.Bounds.Width is > 0 ? parent.Bounds.Width : _barMaxWidth;
            bar.Width = _barMaxWidth * (pct / 100.0);

            // Color: red when muted, green→yellow→red gradient based on level
            bar.Background = muted
                ? new SolidColorBrush(Color.Parse("#EF5350"))
                : pct > 80
                    ? new SolidColorBrush(Color.Parse("#FFB74D"))
                    : new SolidColorBrush(Color.Parse("#4FC3F7"));
        }

        // Mute indicator ring
        if (this.FindControl<Border>("MuteIndicator") is { } indicator)
            indicator.Background = muted
                ? new SolidColorBrush(Color.Parse("#40EF5350"))
                : new SolidColorBrush(Color.Parse("#1AFFFFFF"));

        // Status badge
        if (this.FindControl<Border>("StatusBadge") is { } badge)
            badge.Background = muted
                ? new SolidColorBrush(Color.Parse("#1AEF5350"))
                : new SolidColorBrush(Color.Parse("#1A4FC3F7"));

        if (this.FindControl<TextBlock>("StatusText") is { } status)
        {
            status.Text       = muted ? "MUTED" : "ACTIVE";
            status.Foreground = muted
                ? new SolidColorBrush(Color.Parse("#EF5350"))
                : new SolidColorBrush(Color.Parse("#4FC3F7"));
        }

        // dB readout
        if (this.FindControl<TextBlock>("DbText") is { } dbTb)
            dbTb.Text = db <= -59 ? "—" : $"{db:F1}";
    }
}
