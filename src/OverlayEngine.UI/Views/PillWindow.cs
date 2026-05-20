using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using OverlayEngine.Core.Services;
using OverlayEngine.UI.Platform;
using OverlayEngine.UI.ViewModels;
using ReactiveUI;

namespace OverlayEngine.UI.Views;

public sealed class PillWindow : Window
{
    private readonly IWindowHelper        _helper = WindowHelperFactory.Create();
    private readonly MainOverlayViewModel _vm;
    private readonly ISettingsService     _settings;

    private bool   _isDragging;
    private Point  _dragStartScreen;
    private PixelPoint _winStart;

    public PillWindow(MainOverlayViewModel vm, ISettingsService settings)
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

        Content = new PillView { DataContext = vm };

        var islandLayout = settings.Current.Widgets.FirstOrDefault(w => w.Id == "island");
        Position = islandLayout is not null
            ? new PixelPoint((int)islandLayout.X, (int)islandLayout.Y)
            : new PixelPoint(100, 20);

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
        SavePosition();
    }

    private void SavePosition()
    {
        var layout = _settings.Current.Widgets.FirstOrDefault(w => w.Id == "island");
        if (layout is null) return;
        layout.X = Position.X;
        layout.Y = Position.Y;
        Task.Run(_settings.SaveAsync);
    }
}
