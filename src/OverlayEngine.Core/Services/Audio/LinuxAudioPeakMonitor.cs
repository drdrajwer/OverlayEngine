using System.Diagnostics;

namespace OverlayEngine.Core.Services.Audio;

internal sealed class LinuxAudioPeakMonitor : IDisposable
{
    private Process? _process;
    private volatile int _rawPeak; // 0..32767
    private readonly CancellationTokenSource _cts = new();
    private bool _available;

    /// <summary>Peak level 0.0–1.0, or -1.0 if parec is unavailable.</summary>
    public double PeakLevel => _available ? _rawPeak / 32767.0 : -1.0;

    public void Start()
    {
        try
        {
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName               = "parec",
                    Arguments              = "--channels=1 --rate=8000 --format=s16le --latency-msec=50",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                }
            };
            _process.Start();
            _available = true;
            Task.Run(() => ReadLoopAsync(_cts.Token));
        }
        catch { _available = false; }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        if (_process is null) return;
        var stream = _process.StandardOutput.BaseStream;
        var buf = new byte[3200]; // ~200 ms at 8 kHz mono s16le
        try
        {
            while (!ct.IsCancellationRequested && !_process.HasExited)
            {
                int n = await stream.ReadAsync(buf.AsMemory(), ct);
                if (n < 2) continue;

                int peak = 0;
                for (int i = 0; i + 1 < n; i += 2)
                {
                    int s = Math.Abs((int)BitConverter.ToInt16(buf, i));
                    if (s > peak) peak = s;
                }
                _rawPeak = peak; // volatile write — int writes are atomic on all .NET platforms
            }
        }
        catch (OperationCanceledException) { }
        catch { _available = false; }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _process?.Kill(); } catch { }
        _process?.Dispose();
        _cts.Dispose();
    }
}
