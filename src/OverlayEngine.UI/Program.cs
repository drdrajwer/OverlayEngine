using Avalonia;
using Avalonia.ReactiveUI;

namespace OverlayEngine.UI;

class Program
{
    internal static readonly string LogPath =
        Path.Combine(Path.GetTempPath(), "oe_debug.log");

    internal const string AppVersion = "1.1.0";

    [STAThread]
    public static void Main(string[] args)
    {
        File.WriteAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] Main started v{AppVersion}\n");
        try
        {
            var builder = BuildAvaloniaApp();
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] AppBuilder created\n");
            builder.StartWithClassicDesktopLifetime(args);
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] Exited normally\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] FATAL: {ex}\n");
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseReactiveUI()
            .WithInterFont()
            .LogToTrace();
}
