using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace OverlayEngine.UI.Views.Widgets;

/// <summary>
/// Circular progress ring drawn via Avalonia's ICustomDrawOperation for GPU-accelerated rendering.
/// Animated via a smooth eased value that chases the target.
/// </summary>
public partial class RingMeter : UserControl
{
    // ── Avalonia Properties ───────────────────────────────────────────────

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<RingMeter, double>(nameof(Value), 0.0);

    public static readonly StyledProperty<double> MaxValueProperty =
        AvaloniaProperty.Register<RingMeter, double>(nameof(MaxValue), 100.0);

    public static readonly StyledProperty<IBrush> GlowBrushProperty =
        AvaloniaProperty.Register<RingMeter, IBrush>(nameof(GlowBrush),
            new SolidColorBrush(Color.Parse("#4FC3F7")));

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<RingMeter, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> SubLabelProperty =
        AvaloniaProperty.Register<RingMeter, string>(nameof(SubLabel), string.Empty);

    public double  Value     { get => GetValue(ValueProperty);     set => SetValue(ValueProperty, value); }
    public double  MaxValue  { get => GetValue(MaxValueProperty);  set => SetValue(MaxValueProperty, value); }
    public IBrush  GlowBrush { get => GetValue(GlowBrushProperty); set => SetValue(GlowBrushProperty, value); }
    public string  Label     { get => GetValue(LabelProperty);     set => SetValue(LabelProperty, value); }
    public string  SubLabel  { get => GetValue(SubLabelProperty);  set => SetValue(SubLabelProperty, value); }

    // ── Smooth animation state ─────────────────────────────────────────────
    private double _displayValue;
    private DispatcherTimer? _animTimer;

    public RingMeter()
    {
        InitializeComponent();

        // Smooth chase animation at ~60 fps
        _animTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _animTimer.Tick += (_, _) =>
        {
            var target = MaxValue > 0 ? Value / MaxValue : 0;
            _displayValue += (target - _displayValue) * 0.12; // exponential ease
            InvalidateVisual();
            UpdateLabels();
        };
        _animTimer.Start();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LabelProperty)
        {
            if (this.FindControl<TextBlock>("LabelText") is { } tb) tb.Text = Label;
        }
        if (change.Property == SubLabelProperty)
        {
            if (this.FindControl<TextBlock>("SubLabelText") is { } tb) tb.Text = SubLabel;
        }
    }

    private void UpdateLabels()
    {
        if (this.FindControl<TextBlock>("ValueText") is { } tb)
            tb.Text = $"{Value:F0}%";
        if (this.FindControl<TextBlock>("SubLabelText") is { } tb2)
            tb2.Text = SubLabel;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        const double strokeWidth = 7.0;
        const double padding     = 6.0;
        var size   = Math.Min(Bounds.Width, Bounds.Height);
        var radius = (size / 2.0) - padding - (strokeWidth / 2.0);
        var center = new Point(Bounds.Width / 2.0, Bounds.Height / 2.0);

        // Background track
        using (context.PushOpacity(0.2))
        {
            var trackPen = new Pen(GlowBrush, strokeWidth, lineCap: PenLineCap.Round);
            DrawArc(context, trackPen, center, radius, -90, 360);
        }

        // Foreground arc
        if (_displayValue > 0.001)
        {
            var arcPen = new Pen(GlowBrush, strokeWidth, lineCap: PenLineCap.Round);
            DrawArc(context, arcPen, center, radius, -90, _displayValue * 360.0);
        }
    }

    private static void DrawArc(DrawingContext ctx, Pen pen,
                                 Point center, double radius,
                                 double startDeg, double sweepDeg)
    {
        var geometry = BuildArcGeometry(center, radius, startDeg, sweepDeg);
        ctx.DrawGeometry(null, pen, geometry);
    }

    private static StreamGeometry BuildArcGeometry(Point center, double radius,
                                                    double startDeg, double sweepDeg)
    {
        sweepDeg = Math.Min(sweepDeg, 359.9999); // avoid degenerate full circle
        var geo = new StreamGeometry();
        using var ctx = geo.Open();

        var startRad = startDeg * Math.PI / 180.0;
        var endRad   = (startDeg + sweepDeg) * Math.PI / 180.0;

        var startPt = new Point(
            center.X + radius * Math.Cos(startRad),
            center.Y + radius * Math.Sin(startRad));
        var endPt = new Point(
            center.X + radius * Math.Cos(endRad),
            center.Y + radius * Math.Sin(endRad));

        ctx.BeginFigure(startPt, false);
        ctx.ArcTo(endPt, new Size(radius, radius),
                  rotationAngle: 0,
                  isLargeArc: sweepDeg > 180,
                  sweepDirection: SweepDirection.Clockwise);
        ctx.EndFigure(false);

        return geo;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _animTimer?.Stop();
        _animTimer = null;
    }
}
