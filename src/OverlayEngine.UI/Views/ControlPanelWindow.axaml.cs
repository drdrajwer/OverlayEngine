using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using OverlayEngine.UI.ViewModels.ControlPanel;

namespace OverlayEngine.UI.Views;

public partial class ControlPanelWindow : Window
{
    public ControlPanelWindow()
    {
        InitializeComponent();
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://OverlayEngine.UI/Assets/icon.png"));
            Icon = new WindowIcon(stream);
        }
        catch { }
    }

    public ControlPanelWindow(SettingsPanelViewModel vm) : this()
        => DataContext = vm;

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        RefreshMonitors();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // X button (WindowClosing) → hide only; ApplicationShutdown → allow → app exits
        if (e.CloseReason == WindowCloseReason.WindowClosing)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void OnRefreshMonitors(object? sender, RoutedEventArgs e) => RefreshMonitors();

    private void RefreshMonitors()
    {
        if (DataContext is not SettingsPanelViewModel vm) return;
        vm.AvailableMonitors.Clear();
        foreach (var (screen, i) in Screens.All.Select((s, i) => (s, i)))
        {
            var b = screen.Bounds;
            vm.AvailableMonitors.Add($"Monitor {i + 1}  ({b.Width}×{b.Height})");
        }
        if (vm.AvailableMonitors.Count > 0 && vm.SelectedMonitorIndex >= vm.AvailableMonitors.Count)
            vm.SelectedMonitorIndex = 0;
    }
}
