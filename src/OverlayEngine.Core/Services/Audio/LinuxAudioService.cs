using System.Diagnostics;
using System.Text.RegularExpressions;

namespace OverlayEngine.Core.Services.Audio;

public sealed class LinuxAudioService : IAudioService
{
    private static readonly Regex VolumeRegex = new(@"Volume:.*?(\d+)%", RegexOptions.Compiled);
    private static readonly Regex MuteRegex   = new(@"Mute:\s*(yes|no)",  RegexOptions.Compiled);

    private readonly LinuxAudioPeakMonitor _peak = new();

    public LinuxAudioService() => _peak.Start();

    public double PeakLevel => _peak.PeakLevel;

    public async Task<AudioSnapshot> ReadAsync()
    {
        try
        {
            var output = await RunAsync("pactl", "get-source-volume @DEFAULT_SOURCE@");
            if (output is null) return new AudioSnapshot(0, false);

            var volMatch  = VolumeRegex.Match(output);
            var muteOut   = await RunAsync("pactl", "get-source-mute @DEFAULT_SOURCE@");
            var muteMatch = muteOut is not null ? MuteRegex.Match(muteOut) : Match.Empty;

            double vol   = volMatch.Success  ? double.Parse(volMatch.Groups[1].Value) : 0;
            bool   muted = muteMatch.Success && muteMatch.Groups[1].Value == "yes";

            double db = vol > 0 ? 20.0 * Math.Log10(vol / 100.0) : -60;
            return new AudioSnapshot(db, muted);
        }
        catch { return new AudioSnapshot(-60, false); }
    }

    private static async Task<string?> RunAsync(string cmd, string args)
    {
        using var process = new Process
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
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return process.ExitCode == 0 ? output : null;
    }

    public void Dispose() => _peak.Dispose();
}
