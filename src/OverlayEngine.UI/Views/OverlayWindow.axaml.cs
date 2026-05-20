using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using OverlayEngine.UI.Platform;
using OverlayEngine.UI.ViewModels;
using ReactiveUI;

namespace OverlayEngine.UI.Views;

public partial class OverlayWindow : Window
{
    private readonly IWindowHelper _windowHelper = WindowHelperFactory.Create();
    private MainOverlayViewModel?  _vm;

    // Drag state
    private bool   _isDragging;
    private Point  _dragStartPointer;
    private Point  _dragStartWindow;

    public OverlayWindow()
    {
        InitializeComponent();

        // Set transparency hint via code — AXAML requires IReadOnlyList<WindowTransparencyLevel>
        TransparencyLevelHint =
        [
            WindowTransparencyLevel.AcrylicBlur,
            WindowTransparencyLevel.Blur,
            WindowTransparencyLevel.Transparent
        ];
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _vm = DataContext as MainOverlayViewModel;

        if (_vm is not null)
        {
            // React to edit-mode changes → toggle click-through
            _vm.WhenAnyValue(x => x.IsEditMode)
               .Subscribe(editMode => ApplyClickThrough(!editMode));
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Start locked (click-through enabled) by default
        ApplyClickThrough(enabled: _vm?.IsEditMode == false);
    }

    // ── Drag-to-move (only works when IsEditMode = true) ──────────────────────

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_vm?.IsEditMode != true) return;

        _isDragging       = true;
        _dragStartPointer = e.GetPosition(this);
        _dragStartWindow  = new Point(Position.X, Position.Y);
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isDragging) return;

        var current = e.GetPosition(this);
        var delta   = current - _dragStartPointer;
        Position = new PixelPoint(
            (int)(_dragStartWindow.X + delta.X),
            (int)(_dragStartWindow.Y + delta.Y));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_isDragging) return;
        _isDragging = false;
        e.Pointer.Capture(null);
        PersistPosition();
    }

    // ── Click-through implementation (cross-platform) ─────────────────────────

    private void ApplyClickThrough(bool enabled)
    {
        // TryGetPlatformHandle is the Avalonia 11 API to get the native window handle
        var handle = TryGetPlatformHandle();
        if (handle is null) return;

        if (enabled)
            _windowHelper.EnableClickThrough(handle.Handle);
        else
            _windowHelper.DisableClickThrough(handle.Handle);
    }

    private void PersistPosition()
    {
        if (_vm is null) return;
        // ViewModel can optionally save widget position back to settings
        // Layout saving is wired through MainOverlayViewModel → ISettingsService
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _vm?.Dispose();
    }
}
