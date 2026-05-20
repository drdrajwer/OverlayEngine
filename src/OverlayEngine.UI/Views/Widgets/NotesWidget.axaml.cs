using Avalonia.Controls;
using Avalonia.ReactiveUI;
using OverlayEngine.UI.ViewModels.Widgets;
using ReactiveUI;

namespace OverlayEngine.UI.Views.Widgets;

public partial class NotesWidget : UserControl, IViewFor<NotesWidgetViewModel>
{
    public NotesWidgetViewModel? ViewModel { get; set; }
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (NotesWidgetViewModel?)value;
    }

    public NotesWidget() => InitializeComponent();
}
