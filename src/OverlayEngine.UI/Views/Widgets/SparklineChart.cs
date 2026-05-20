using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace OverlayEngine.UI.Views.Widgets;

/// <summary>
/// Lightweight GPU-rendered sparkline that draws two polylines (download/upload).
/// Redraw is triggered only when the bound collections change, keeping CPU usage minimal.
/// </summary>
public sealed class SparklineChart : Control
{
    public static readonly StyledProperty<ObservableCollection<double>> DownloadHistoryProperty =
        AvaloniaProperty.Register<SparklineChart, ObservableCollection<double>>(
            nameof(DownloadHistory), []);

    public static readonly StyledProperty<ObservableCollection<double>> UploadHistoryProperty =
        AvaloniaProperty.Register<SparklineChart, ObservableCollection<double>>(
            nameof(UploadHistory), []);

    public ObservableCollection<double> DownloadHistory
    {
        get => GetValue(DownloadHistoryProperty);
        set => SetValue(DownloadHistoryProperty, value);
    }

    public ObservableCollection<double> UploadHistory
    {
        get => GetValue(UploadHistoryProperty);
        set => SetValue(UploadHistoryProperty, value);
    }

    private static readonly IBrush DownBrush = new SolidColorBrush(Color.Parse("#4FC3F7"));
    private static readonly IBrush UpBrush   = new SolidColorBrush(Color.Parse("#CE93D8"));
    private static readonly IBrush FillDown  = new SolidColorBrush(Color.FromArgb(30, 0x4F, 0xC3, 0xF7));
    private static readonly IBrush FillUp    = new SolidColorBrush(Color.FromArgb(30, 0xCE, 0x93, 0xD8));

    public SparklineChart()
    {
        DownloadHistory.CollectionChanged += OnDataChanged;
        UploadHistory.CollectionChanged   += OnDataChanged;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DownloadHistoryProperty)
        {
            if (change.OldValue is ObservableCollection<double> old)
                old.CollectionChanged -= OnDataChanged;
            if (change.NewValue is ObservableCollection<double> @new)
                @new.CollectionChanged += OnDataChanged;
        }
        if (change.Property == UploadHistoryProperty)
        {
            if (change.OldValue is ObservableCollection<double> old)
                old.CollectionChanged -= OnDataChanged;
            if (change.NewValue is ObservableCollection<double> @new)
                @new.CollectionChanged += OnDataChanged;
        }
    }

    private void OnDataChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w < 2 || h < 2) return;

        RenderLine(context, DownloadHistory, DownBrush, FillDown, w, h);
        RenderLine(context, UploadHistory,   UpBrush,   FillUp,   w, h);
    }

    private static void RenderLine(DrawingContext ctx,
                                   ObservableCollection<double> data,
                                   IBrush lineBrush, IBrush fillBrush,
                                   double w, double h)
    {
        if (data.Count < 2) return;

        var max = data.Max();
        if (max <= 0) max = 1;

        var stepX  = w / (data.Count - 1);
        var points = data
            .Select((v, i) => new Point(i * stepX, h - (v / max) * h * 0.9))
            .ToList();

        // Fill geometry
        var fillGeo = new StreamGeometry();
        using (var fillCtx = fillGeo.Open())
        {
            fillCtx.BeginFigure(new Point(0, h), true);
            foreach (var pt in points) fillCtx.LineTo(pt);
            fillCtx.LineTo(new Point(w, h));
            fillCtx.EndFigure(true);
        }
        ctx.DrawGeometry(fillBrush, null, fillGeo);

        // Line geometry
        var lineGeo = new StreamGeometry();
        using (var lineCtx = lineGeo.Open())
        {
            lineCtx.BeginFigure(points[0], false);
            for (var i = 1; i < points.Count; i++) lineCtx.LineTo(points[i]);
            lineCtx.EndFigure(false);
        }
        ctx.DrawGeometry(null, new Pen(lineBrush, 1.5), lineGeo);
    }
}
