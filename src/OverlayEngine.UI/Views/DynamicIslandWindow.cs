using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using OverlayEngine.Core.Services;
using OverlayEngine.UI.Platform;
using OverlayEngine.UI.ViewModels;
using OverlayEngine.UI.Views.Widgets;
using ReactiveUI;

namespace OverlayEngine.UI.Views;

public sealed class DynamicIslandWindow : Window
{
    private readonly IWindowHelper        _helper = WindowHelperFactory.Create();
    private readonly MainOverlayViewModel _vm;
    private readonly ISettingsService     _settings;

    private bool   _isDragging;
    private Point  _dragStartScreen;
    private PixelPoint _winStart;

    public DynamicIslandWindow(MainOverlayViewModel vm, ISettingsService settings)
    {
        _vm       = vm;
        _settings = settings;

        SystemDecorations                 = SystemDecorations.None;
        Background                        = Brushes.Transparent;
        TransparencyLevelHint             = [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent];
        Topmost                           = true;
        ShowInTaskbar                     = false;
        SizeToContent                     = SizeToContent.WidthAndHeight;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints       = ExtendClientAreaChromeHints.NoChrome;

        // Override tile appearance for the island — transparent bg, no borders, compact padding
        Styles.Add(new Style(s => s.OfType<Border>().Class("tile"))
        {
            Setters =
            {
                new Setter(Border.BackgroundProperty,      Brushes.Transparent),
                new Setter(Border.BorderThicknessProperty, new Thickness(0)),
                new Setter(Border.BoxShadowProperty,       new BoxShadows()),
                new Setter(Border.PaddingProperty,         new Thickness(12, 7)),
                new Setter(Border.CornerRadiusProperty,    new CornerRadius(0)),
            }
        });

        Border Sep() => new()
        {
            Height              = 1,
            Background          = new SolidColorBrush(Color.Parse("#18FFFFFF")),
            Margin              = new Thickness(10, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var cpuWidget       = new CpuGpuWidget    { DataContext = vm.CpuGpu      };
        var audioWidget     = new AudioWidget     { DataContext = vm.Audio       };
        var netWidget       = new NetworkWidget   { DataContext = vm.Network     };
        var notesWidget     = new NotesWidget     { DataContext = vm.Notes       };
        var nowPlayingWidget= new NowPlayingWidget{ DataContext = vm.NowPlaying  };
        var clockWidget     = new Widgets.ClockWidget { DataContext = vm.Clock   };

        var sep1 = Sep(); var sep2 = Sep(); var sep3 = Sep(); var sep4 = Sep(); var sep5 = Sep();

        vm.WhenAnyValue(x => x.ShowCpuGpu)     .Subscribe(v => { cpuWidget.IsVisible       = v; sep1.IsVisible = v; });
        vm.WhenAnyValue(x => x.ShowAudio)      .Subscribe(v => { audioWidget.IsVisible      = v; sep2.IsVisible = v; });
        vm.WhenAnyValue(x => x.ShowNetwork)    .Subscribe(v => { netWidget.IsVisible        = v; sep3.IsVisible = v; });
        vm.WhenAnyValue(x => x.ShowNotes)      .Subscribe(v =>   notesWidget.IsVisible      = v);
        vm.WhenAnyValue(x => x.ShowNowPlaying) .Subscribe(v => { nowPlayingWidget.IsVisible = v; sep4.IsVisible = v; });
        vm.WhenAnyValue(x => x.ShowClock)      .Subscribe(v => { clockWidget.IsVisible      = v; sep5.IsVisible = v; });

        var stack = new StackPanel
        {
            Spacing = 0,
            Width   = 270,
            Children = { cpuWidget, sep1, audioWidget, sep2, netWidget, sep3, notesWidget, sep4, nowPlayingWidget, sep5, clockWidget }
        };

        var island = new Border
        {
            Background    = new SolidColorBrush(Color.Parse("#F2040404")),
            CornerRadius  = new CornerRadius(26),
            ClipToBounds  = true,
            BoxShadow     = BoxShadows.Parse("0 12 56 0 #99000000, 0 2 8 0 #66000000"),
            Child         = stack,
        };

        Content = new Panel { Children = { island } };

        var islandLayout = settings.Current.Widgets.FirstOrDefault(w => w.Id == "island");
        Position = islandLayout is not null
            ? new PixelPoint((int)islandLayout.X, (int)islandLayout.Y)
            : new PixelPoint(60, 60);

        vm.WhenAnyValue(x => x.IsEditMode).Subscribe(SetEditMode);
    }

    private void SetEditMode(bool edit)
    {
        var handle = TryGetPlatformHandle();
        if (handle is null) return;
        if (edit) _helper.DisableClickThrough(handle.Handle);
        else      _helper.EnableClickThrough(handle.Handle);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        SetEditMode(_vm.IsEditMode);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!_vm.IsEditMode) return;
        _isDragging = true;
        var rel = e.GetPosition(this);
        _dragStartScreen = new Point(Position.X + rel.X, Position.Y + rel.Y);
        _winStart = Position;
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isDragging) return;
        var rel = e.GetPosition(this);
        var cur = new Point(Position.X + rel.X, Position.Y + rel.Y);
        Position = new PixelPoint(
            _winStart.X + (int)(cur.X - _dragStartScreen.X),
            _winStart.Y + (int)(cur.Y - _dragStartScreen.Y));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_isDragging) return;
        _isDragging = false;
        e.Pointer.Capture(null);
        SaveIslandPosition();
    }

    private void SaveIslandPosition()
    {
        var layout = _settings.Current.Widgets.FirstOrDefault(w => w.Id == "island");
        if (layout is null) return;
        layout.X = Position.X;
        layout.Y = Position.Y;
        Task.Run(_settings.SaveAsync);
    }
}
