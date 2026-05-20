using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using OverlayEngine.Core.Models;
using OverlayEngine.Core.Services;
using OverlayEngine.UI.Platform;
using OverlayEngine.UI.ViewModels;
using ReactiveUI;

namespace OverlayEngine.UI.Views;

public sealed class WidgetWindow : Window
{
    private readonly IWindowHelper          _helper  = WindowHelperFactory.Create();
    private readonly MainOverlayViewModel   _vm;
    private readonly ISettingsService       _settings;
    private readonly int                    _widgetIndex;
    private readonly LayoutTransformControl _scaleHost;
    private readonly Border                 _editBorder;
    private readonly TextBlock              _dragHint;

    private bool   _isDragging;
    private Point  _dragStartScreen; // screen coords — avoids drift when window moves
    private PixelPoint _winStart;

    public WidgetWindow(Control widget, MainOverlayViewModel vm, int widgetIndex, ISettingsService settings)
    {
        _vm          = vm;
        _settings    = settings;
        _widgetIndex = widgetIndex;

        SystemDecorations              = SystemDecorations.None;
        Background                     = Brushes.Transparent;
        TransparencyLevelHint          = [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent];
        Topmost                        = true;
        ShowInTaskbar                  = false;
        SizeToContent                  = SizeToContent.WidthAndHeight;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints    = ExtendClientAreaChromeHints.NoChrome;

        _scaleHost = new LayoutTransformControl
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment   = VerticalAlignment.Top,
            Child               = widget,
        };

        _editBorder = new Border
        {
            BorderBrush      = new SolidColorBrush(Color.Parse("#884FC3F7")),
            BorderThickness  = new Thickness(1.5),
            CornerRadius     = new CornerRadius(12),
            IsHitTestVisible = false,
            IsVisible        = false,
        };

        _dragHint = new TextBlock
        {
            Text                = "⣿ przesuń",
            FontSize            = 8,
            Foreground          = new SolidColorBrush(Color.Parse("#664FC3F7")),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment   = VerticalAlignment.Top,
            Margin              = new Thickness(0, 4, 8, 0),
            IsVisible           = false,
            IsHitTestVisible    = false,
            FontFamily          = new FontFamily("Inter,Roboto,sans-serif"),
        };

        var panel = new Panel();
        panel.Children.Add(_scaleHost);
        panel.Children.Add(_editBorder);
        panel.Children.Add(_dragHint);
        Content = panel;

        ApplyScale(settings.Current.Scale);

        var layouts = settings.Current.Widgets;
        if (widgetIndex < layouts.Count)
            Position = new PixelPoint((int)layouts[widgetIndex].X, (int)layouts[widgetIndex].Y);
        else
            Position = new PixelPoint(20, 20 + widgetIndex * 140);

        vm.WhenAnyValue(x => x.IsEditMode).Subscribe(SetEditMode);
        vm.WhenAnyValue(x => x.Scale).Subscribe(ApplyScale);
    }

    private void ApplyScale(double scale)
    {
        _scaleHost.LayoutTransform = new ScaleTransform(scale, scale);
    }

    private void SetEditMode(bool edit)
    {
        _editBorder.IsVisible = edit;
        _dragHint.IsVisible   = edit;
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
        var layouts = _settings.Current.Widgets;
        if (_widgetIndex >= layouts.Count) return;
        var layout = layouts[_widgetIndex];
        layout.X = Position.X;
        layout.Y = Position.Y;
        Task.Run(_settings.SaveAsync);
    }
}
