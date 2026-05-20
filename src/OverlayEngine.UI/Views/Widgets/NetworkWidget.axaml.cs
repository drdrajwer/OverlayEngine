using Avalonia.Controls;
using Avalonia.ReactiveUI;
using OverlayEngine.UI.ViewModels.Widgets;
using ReactiveUI;

namespace OverlayEngine.UI.Views.Widgets;

public partial class NetworkWidget : UserControl, IViewFor<NetworkWidgetViewModel>
{
    public NetworkWidgetViewModel? ViewModel { get; set; }
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (NetworkWidgetViewModel?)value;
    }

    public NetworkWidget() => InitializeComponent();
}
