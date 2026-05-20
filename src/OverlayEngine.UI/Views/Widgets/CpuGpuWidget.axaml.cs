using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using OverlayEngine.UI.ViewModels.Widgets;
using ReactiveUI;

namespace OverlayEngine.UI.Views.Widgets;

public partial class CpuGpuWidget : UserControl, IViewFor<CpuGpuWidgetViewModel>
{
    public CpuGpuWidgetViewModel? ViewModel { get; set; }
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (CpuGpuWidgetViewModel?)value;
    }

    public CpuGpuWidget()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            if (DataContext is not CpuGpuWidgetViewModel vm) return;

            d.Add(vm.WhenAnyValue(x => x.CpuGlow)
                    .Subscribe(brush =>
                    {
                        if (this.FindControl<Border>("TileBorder") is { } border)
                            border.BorderBrush = brush;
                    }));
        });
    }
}
