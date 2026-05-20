using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

namespace OverlayEngine.Core.Services.Network;

public sealed class NetworkService : INetworkService
{
    private long _prevRx;
    private long _prevTx;
    private DateTime _prevTime = DateTime.UtcNow;

    public async Task<NetworkSnapshot> ReadAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return await ReadLinuxAsync();
        return ReadWindowsCrossplatform();
    }

    private async Task<NetworkSnapshot> ReadLinuxAsync()
    {
        try
        {
            var lines = await File.ReadAllLinesAsync("/proc/net/dev");
            long totalRx = 0, totalTx = 0;

            foreach (var line in lines.Skip(2)) // skip header rows
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("lo:")) continue; // skip loopback

                var colonIdx = trimmed.IndexOf(':');
                if (colonIdx < 0) continue;

                var parts = trimmed[(colonIdx + 1)..].Split(' ',
                    StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 9) continue;

                if (long.TryParse(parts[0], out var rx)) totalRx += rx; // bytes received
                if (long.TryParse(parts[8], out var tx)) totalTx += tx; // bytes transmitted
            }

            return ComputeRate(totalRx, totalTx);
        }
        catch { return new NetworkSnapshot(0, 0); }
    }

    private NetworkSnapshot ReadWindowsCrossplatform()
    {
        try
        {
            long totalRx = 0, totalTx = 0;
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                var stats = nic.GetIPv4Statistics();
                totalRx += stats.BytesReceived;
                totalTx += stats.BytesSent;
            }
            return ComputeRate(totalRx, totalTx);
        }
        catch { return new NetworkSnapshot(0, 0); }
    }

    private NetworkSnapshot ComputeRate(long currentRx, long currentTx)
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _prevTime).TotalSeconds;
        if (elapsed < 0.001) elapsed = 0.001;

        double dl = (_prevRx > 0) ? Math.Max(0, (currentRx - _prevRx) / elapsed) : 0;
        double ul = (_prevTx > 0) ? Math.Max(0, (currentTx - _prevTx) / elapsed) : 0;

        _prevRx   = currentRx;
        _prevTx   = currentTx;
        _prevTime = now;

        return new NetworkSnapshot(dl, ul);
    }

    public void Dispose() { }
}
