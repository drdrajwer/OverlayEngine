using System.Diagnostics;

namespace OverlayEngine.Core.Services.NowPlaying;

/// <summary>
/// Reads currently playing track via playerctl (MPRIS D-Bus proxy).
/// Works with Spotify, browsers, most media players on PipeWire/GNOME desktops.
/// </summary>
public sealed class LinuxNowPlayingService : INowPlayingService
{
    public async Task<NowPlayingSnapshot> ReadAsync()
    {
        try
        {
            var status = await RunAsync("playerctl", "status");
            if (status is null || !status.Trim().Equals("Playing", StringComparison.OrdinalIgnoreCase))
                return new NowPlayingSnapshot(null, null, false);

            var artist = (await RunAsync("playerctl", "metadata artist"))?.Trim() ?? "";
            var title  = (await RunAsync("playerctl", "metadata title"))?.Trim()  ?? "";
            return new NowPlayingSnapshot(artist, title, true);
        }
        catch { return new NowPlayingSnapshot(null, null, false); }
    }

    private static async Task<string?> RunAsync(string cmd, string args)
    {
        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName               = cmd,
                Arguments              = args,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            }
        };
        proc.Start();
        var output = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();
        return proc.ExitCode == 0 ? output : null;
    }

    public void Dispose() { }
}
